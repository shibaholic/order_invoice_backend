namespace HospitalSupply.Entities;

public class ItemOrder
{
    public int Id { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public decimal? CurrencyAmount { get; set; }
    public string? CurrencyCode { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }
}