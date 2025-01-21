namespace HospitalSupply.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public string ContentType { get; set; }
    public List<ItemOrder> ItemOrders { get; set; }
    public DateTime DateCreated { get; set; }
}