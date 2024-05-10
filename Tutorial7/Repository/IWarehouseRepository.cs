namespace Tutorial7.Repository;

public interface IWarehouseRepository
{
    Task<bool> DoesWarehouseExist(int warehouseId);
}