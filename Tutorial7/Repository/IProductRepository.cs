using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository;

public interface IProductRepository
{
    Task<bool> DoesProductExist(int productId);

    Task<GetProductDto> GetProduct(int productId);
}