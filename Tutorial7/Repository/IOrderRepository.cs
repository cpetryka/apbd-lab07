using Tutorial7.Controller.Dto;

namespace Tutorial7.Repository;

public interface IOrderRepository
{
    Task<bool> DoesOrderForProductWithGivenAmountExist(CreateProductWarehouseDto createProductWarehouseDto);
    Task<bool> IsOrderForProductWithGivenAmountAlreadyFulfilled(int orderId);

    Task<int> UpdateFullfilledAt(int orderId, DateTime fullfilledAt);
}