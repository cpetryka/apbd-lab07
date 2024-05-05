using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial7.Controller.Dto;

namespace Tutorial7.Controller;

[ApiController]
[Route(("api/[controller]"))]
public class WarehouseController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WarehouseController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public IActionResult AddProductWarehouse(CreateProductWarehouseDto createProductWarehouseDto)
    {
        // ---------------------------------------------------------------------------------------------------
        // Tak naprawdę większość poniższego kodu powinno być wydzielone do repo, ewentualnie jakichs serwisow itp.,
        // jednak dla przyspieszenia pracy (i dlatego, ze musze zrobic tylko jeden endpoint) umieszczam calosc to tutaj.
        // ---------------------------------------------------------------------------------------------------

        try
        {
            // Otwieramy połączenie i tworzymy nową transakcję
            using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
            connection.Open();
            var transaction = connection.BeginTransaction();

            // 1
            // Sprawdzenie czy produkt i magazyn istnieją
            using var productExistsCommand = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @IdProduct", connection, transaction);
            productExistsCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);

            using (var productReader = productExistsCommand.ExecuteReader())
            {
                if (!productReader.Read())
                {
                    return NotFound("Product not found.");
                }
            }

            using var warehouseExistsCommand = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection, transaction);
            warehouseExistsCommand.Parameters.AddWithValue("@IdWarehouse", createProductWarehouseDto.IdWarehouse);

            using (var warehouseReader = warehouseExistsCommand.ExecuteReader())
            {
                if (!warehouseReader.Read())
                {
                    return NotFound("Warehouse not found.");
                }
            }


            // Sprawdzenie czy ilość produktu jest większa od zera
            if (createProductWarehouseDto.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            // 2
            // Sprawdzenie czy istnieje zamówienie na produkt
            var orderCommand =
                new SqlCommand(
                    "SELECT COUNT(*) FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Quantity AND CreatedAt < @CurrentDateTime",
                    connection, transaction);
            orderCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
            orderCommand.Parameters.AddWithValue("@Quantity", createProductWarehouseDto.Amount);
            orderCommand.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
            var orderCount = (int)orderCommand.ExecuteScalar();

            if (orderCount == 0)
            {
                return BadRequest("There is no purchase order for the specified product with given quantity.");
            }

            // 3
            // Sprawdzenie czy zamówienie zostało zrealizowane
            var fulfilledCommand =
                new SqlCommand(
                    "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder IN (SELECT IdOrder FROM [Order] WHERE IdProduct = @IdProduct) AND IdOrder NOT IN (SELECT IdOrder FROM [Order] WHERE FulfilledAt IS NULL)",
                    connection, transaction);
            fulfilledCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
            var fulfilledCount = (int)fulfilledCommand.ExecuteScalar();

            if (fulfilledCount > 0)
            {
                return BadRequest("The purchase order for the specified product has already been fulfilled.");
            }

            // 4
            // Zaaktualizuj kolumnę FullfilledAt w tabeli Order
            var updateOrderCommand =
                new SqlCommand(
                    "UPDATE [Order] SET FulfilledAt = @CurrentDateTime OUTPUT INSERTED.IdOrder WHERE IdProduct = @IdProduct AND FulfilledAt IS NULL",
                    connection, transaction);
            updateOrderCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
            updateOrderCommand.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
            // updateOrderCommand.ExecuteNonQuery();

            var updatedOrderId = 0;

            using (var reader = updateOrderCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    updatedOrderId = (int)reader["IdOrder"];
                }
            }

            // 5
            // Dodaj rekord do tabeli Product_Warehouse
            var productPriceCommand = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", connection, transaction);
            productPriceCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
            var productPrice = (decimal)productPriceCommand.ExecuteScalar();

            var insertCommand =
                new SqlCommand(
                    "INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, IdOrder, Amount, Price, CreatedAt) VALUES (@IdProduct, @IdWarehouse, @IdOrder, @Amount, @Price, @CurrentDateTime); SELECT SCOPE_IDENTITY();",
                    connection, transaction);
            insertCommand.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
            insertCommand.Parameters.AddWithValue("@IdWarehouse", createProductWarehouseDto.IdWarehouse);
            insertCommand.Parameters.AddWithValue("@IdOrder", updatedOrderId);
            insertCommand.Parameters.AddWithValue("@Amount", createProductWarehouseDto.Amount);
            insertCommand.Parameters.AddWithValue("@Price", createProductWarehouseDto.Amount * productPrice);
            insertCommand.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);

            // 6
            // Wykonaj polecenie INSERT i zwróć wygenerowany klucz główny
            var generatedId = Convert.ToInt32(insertCommand.ExecuteScalar());

            transaction.Commit();
            return Ok(generatedId);
        }
        catch (Exception e)
        {
            Console.Write(e.ToString());
            return StatusCode(500, "Error while processing the request");
        }
    }
}