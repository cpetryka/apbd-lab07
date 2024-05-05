namespace Tutorial7.Controller.Dto;

public class CreateProductWarehouseDto
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    public int Amount { get; set; }
    public string CreatedAt { get; set; }
}