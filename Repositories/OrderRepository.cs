using HospitalSupply.Entities;
using Npgsql;

namespace HospitalSupply.Repositories;

public interface IOrderRepository
{
    Task<int> AddOrder(Order order);
    Task<List<Order>> GetAllOrders();
    Task<Order?> GetOrder(Guid id);
    Task<int> UpdateAsync(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly IDatabase _database;
    public OrderRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<int> AddOrder(Order order)
    {
        var query = "INSERT INTO Orders (Id, SupplierName ) VALUES (@Id, @SupplierName)";
        var parameters = new Dictionary<string, object>
        {
            { "@Id", order.Id },
            { "@SupplierName", order.SupplierName },
        };

        return await _database.ExecuteNonQueryAsync(query, parameters);
    }

    public async Task<List<Order>> GetAllOrders()
    {
        var query = @"SELECT
            myOrder.Id as myOrderId, myOrder.SupplierName, myOrder.InvoiceId as myOrderInvoiceId, myOrder.DateCreated,
            item.Id as itemId, item.ItemName, item.Quantity, item.CurrencyAmount, item.CurrencyCode, item.OrderId, item.InvoiceId as itemInvoiceId
            FROM Orders myOrder 
            LEFT JOIN ItemOrders item ON myOrder.Id = item.OrderId";

        var orderDict = new Dictionary<Guid, Order>();
        
        await using (var command = new NpgsqlCommand(query, _database.GetConnection()))
        {
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var orderId = reader.GetGuid(0);

                    if (!orderDict.TryGetValue(orderId, out var order))
                    {
                        order = new Order
                        {
                            Id = orderId,
                            SupplierName = reader.GetString(1),
                            InvoiceId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
                            DateCreated = reader.GetDateTime(3),
                        };
                        orderDict.Add(orderId, order);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("itemId")))
                    {
                        order.ItemOrders.Add(new ItemOrder
                        {
                            Id = reader.GetInt32(4),
                            ItemName = reader.GetString(5),
                            Quantity = reader.GetInt32(6),
                            CurrencyAmount = reader.GetString(7),
                            CurrencyCode = reader.GetString(8),
                            OrderId = order.Id,
                            InvoiceId = reader.IsDBNull(10) ? null : reader.GetGuid(10)
                        });
                    }
                }
            }
        }

        return orderDict.Values.ToList();
    }

    public async Task<Order?> GetOrder(Guid id)
    {
        var query = $"SELECT * FROM Orders WHERE Id = '{id}'";

        var list = await _database.ExecuteQueryAsync(query, reader => new Order
        {
            Id = reader.GetGuid(0),
            InvoiceId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
            SupplierName = reader.GetString(1),
            DateCreated = reader.GetDateTime(3),
        });

        return list.FirstOrDefault();
    }

    public async Task<int> UpdateAsync(Order order)
    {
        var query = @"
            UPDATE Orders
            SET SupplierName = @SupplierName,
                InvoiceId = @InvoiceId,
                DateCreated = @DateCreated
            WHERE Id = @Id;";
        
        var parameters = new Dictionary<string, object>
        {
            { "@Id", order.Id },
            { "@SupplierName", order.SupplierName },
            { "@InvoiceId", order.InvoiceId },
            { "@DateCreated", order.DateCreated },
        };
        
        return await _database.ExecuteNonQueryAsync(query, parameters);
    }

    // public async Task<List<Order>> GetAllOrders()
    // {
    //     var query = "SELECT * FROM Orders";
    //     
    //     return await _database.ExecuteQueryAsync(query, reader => new Order
    //     {
    //         Id = reader.GetGuid(0),
    //         SupplierName = reader.GetString(1),
    //         InvoiceId = reader.IsDBNull(2) ? null : reader.GetGuid(2),
    //     });
    // }
}