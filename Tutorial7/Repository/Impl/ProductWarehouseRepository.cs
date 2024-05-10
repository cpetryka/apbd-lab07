using Microsoft.Data.SqlClient;
using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository.Impl;

public class ProductWarehouseRepository : IProductWarehouseRepository
{
    private readonly IConfiguration _configuration;
    private readonly IProductRepository _productRepository;

    public ProductWarehouseRepository(IConfiguration configuration, IProductRepository productRepository)
    {
        _configuration = configuration;
        _productRepository = productRepository;
    }

    public async Task<int> AddProductWarehouse(CreateProductWarehouseDto createProductWarehouseDto, int orderId)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "INSERT INTO Product_Warehouse (IdProduct, IdWarehouse, IdOrder, Amount, Price, CreatedAt) VALUES (@IdProduct, @IdWarehouse, @IdOrder, @Amount, @Price, @CurrentDateTime); SELECT SCOPE_IDENTITY();";
        command.Parameters.AddWithValue("@IdProduct", createProductWarehouseDto.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", createProductWarehouseDto.IdWarehouse);
        command.Parameters.AddWithValue("@IdOrder", orderId);
        command.Parameters.AddWithValue("@Amount", createProductWarehouseDto.Amount);
        command.Parameters.AddWithValue("@Price", createProductWarehouseDto.Amount
                                                  * _productRepository.GetProduct(createProductWarehouseDto.IdProduct).Result.Price);
        command.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);

        await connection.OpenAsync();

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}