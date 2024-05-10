using Microsoft.Data.SqlClient;
using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository.Impl;

public class ProductRepository : IProductRepository
{
    private readonly IConfiguration _configuration;

    public ProductRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> DoesProductExist(int productId)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @id";
        command.Parameters.AddWithValue("@id", productId);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

public async Task<GetProductDto> GetProduct(int productId)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = "SELECT Name, Description, Price FROM Product WHERE IdProduct = @productId";
        command.Parameters.AddWithValue("@productId", productId);

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        var nameOrdinal = reader.GetOrdinal("Name");
        var descriptionOrdinal = reader.GetOrdinal("Description");
        var priceOrdinal = reader.GetOrdinal("Price");

        GetProductDto product = null;

        while (await reader.ReadAsync())
        {
            if (product is null)
            {
                product = new GetProductDto
                {
                    IdProduct = productId,
                    Name = reader.GetString(nameOrdinal),
                    Description = reader.GetString(descriptionOrdinal),
                    Price = reader.GetDecimal(priceOrdinal)
                };
            }
        }

        return product;
    }
}