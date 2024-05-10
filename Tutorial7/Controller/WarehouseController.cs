using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial7.Controller.Dto;
using Tutorial7.Repository;

namespace Tutorial7.Controller;

[ApiController]
[Route(("api/[controller]"))]
public class WarehouseController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductWarehouseRepository _productWarehouseRepository;

    public WarehouseController(IConfiguration configuration, IWarehouseRepository warehouseRepository,
        IProductRepository productRepository, IOrderRepository orderRepository,
        IProductWarehouseRepository productWarehouseRepository)
    {
        _configuration = configuration;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _productWarehouseRepository = productWarehouseRepository;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductWarehouse(CreateProductWarehouseDto createProductWarehouseDto)
    {
        // 1
        // Sprawdzenie czy produkt i magazyn istnieją
        if(!await _productRepository.DoesProductExist(createProductWarehouseDto.IdProduct))
        {
            return NotFound("Product not found.");
        }

        if(!await _warehouseRepository.DoesWarehouseExist(createProductWarehouseDto.IdWarehouse))
        {
            return NotFound("Warehouse not found.");
        }

        // Sprawdzenie czy ilość produktu jest większa od zera
        if (createProductWarehouseDto.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        // 2
        // Sprawdzenie czy istnieje zamówienie na produkt
        if(!await _orderRepository.DoesOrderForProductWithGivenAmountExist(createProductWarehouseDto))
        {
            return NotFound("There is no purchase order for the specified product with given quantity.");
        }

        // 3
        // Sprawdzenie czy zamówienie zostało zrealizowane
        if(await _orderRepository.IsOrderForProductWithGivenAmountAlreadyFulfilled(createProductWarehouseDto.IdProduct))
        {
            return BadRequest("The purchase order for the specified product has already been fulfilled.");
        }

        // 4
        // Zaaktualizuj kolumnę FullfilledAt w tabeli Order
        var updatedOrderId = await _orderRepository.UpdateFullfilledAt(createProductWarehouseDto.IdProduct, DateTime.Now);

        // 5
        // Dodaj rekord do tabeli Product_Warehouse
        var generatedId = await _productWarehouseRepository.AddProductWarehouse(createProductWarehouseDto, updatedOrderId);

        return Ok(generatedId);
    }
}