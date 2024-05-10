using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository;

public interface IProductWarehouseRepository
{
    Task<int> AddProductWarehouse(CreateProductWarehouseDto createProductWarehouseDto, int orderId);
}