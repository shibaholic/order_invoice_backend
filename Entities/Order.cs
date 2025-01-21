namespace HospitalSupply.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; }
    public Guid? InvoiceId { get; set; }
    public List<ItemOrder> ItemOrders { get; set; } = new List<ItemOrder>();
    public DateTime DateCreated { get; set; }
}