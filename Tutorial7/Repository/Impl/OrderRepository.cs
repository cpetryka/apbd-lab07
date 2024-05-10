using Microsoft.Data.SqlClient;
using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository.Impl;

public class OrderRepository : IOrderRepository
{
    private readonly IConfiguration _configuration;

    public OrderRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> DoesOrderForProductWithGivenAmountExist(CreateProductWarehouseDto createProductWarehouseDto)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "SELECT COUNT(*) FROM [Order] WHERE IdProduct = @productId AND Amount = @amount AND CreatedAt < @currentDateTime";
        command.Parameters.AddWithValue("@productId", createProductWarehouseDto.IdProduct);
        command.Parameters.AddWithValue("@amount", createProductWarehouseDto.Amount);
        command.Parameters.AddWithValue("@currentDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        await connection.OpenAsync();

        var reader = await command.ExecuteScalarAsync();

        if (reader is null)
        {
            throw new Exception("Error while checking if order for a product exists.");
        }

        return Convert.ToInt32(reader) > 0;
    }

    public async Task<bool> IsOrderForProductWithGivenAmountAlreadyFulfilled(int productId)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "SELECT COUNT(*) FROM Product_Warehouse WHERE IdOrder IN (SELECT IdOrder FROM [Order] WHERE IdProduct = @productId) AND IdOrder NOT IN (SELECT IdOrder FROM [Order] WHERE FulfilledAt IS NULL)";
        command.Parameters.AddWithValue("@productId", productId);

        await connection.OpenAsync();

        var reader = await command.ExecuteScalarAsync();

        if (reader is null)
        {
            throw new Exception("Error while checking if order is already fulfilled.");
        }

        return Convert.ToInt32(reader) > 0;
    }

    public async Task<int> UpdateFullfilledAt(int productId, DateTime fullfilledAt)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "UPDATE [Order] SET FulfilledAt = @fullfilledAt OUTPUT INSERTED.IdOrder WHERE IdProduct = @productId AND FulfilledAt IS NULL";
        command.Parameters.AddWithValue("@productId", productId);
        command.Parameters.AddWithValue("@fullfilledAt", fullfilledAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        await connection.OpenAsync();

        var updatedOrderId = 0;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                updatedOrderId = (int)reader["IdOrder"];
            }
        }

        return updatedOrderId;
    }
}