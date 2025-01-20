namespace HospitalSupply.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public byte[] File { get; set; }
    public List<ItemOrder> ItemOrders { get; set; }
    public DateTime DateCreated { get; set; }
}