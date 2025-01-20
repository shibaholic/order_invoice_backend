using HospitalSupply.Entities;
using Npgsql;

namespace HospitalSupply.Repositories;

public interface IItemOrderRepository
{
    Task<int> AddItemOrders(List<ItemOrder> itemOrders);
}

public class ItemOrderRepository: IItemOrderRepository
{
    private readonly IDatabase _database;
    
    public ItemOrderRepository(IDatabase database)
    {
        _database = database;
    }
    
    public async Task<int> AddItemOrders(List<ItemOrder> itemOrders)
    {
        var values = new List<string>();
        var parameters = new Dictionary<string, object>();

        for (int i = 0; i < itemOrders.Count; i++)
        {
            values.Add($"(@ItemName{i}, @Quantity{i}, @CurrencyAmount{i}, @CurrencyCode{i}, @OrderId{i})");
            parameters.Add($"ItemName{i}", itemOrders[i].ItemName);
            parameters.Add($"Quantity{i}", itemOrders[i].Quantity);
            parameters.Add($"CurrencyAmount{i}", itemOrders[i].CurrencyAmount);
            parameters.Add($"CurrencyCode{i}", itemOrders[i].CurrencyCode);
            parameters.Add($"OrderId{i}", itemOrders[i].OrderId);
        }

        var query = $@"
            INSERT INTO ItemOrders (ItemName, Quantity, CurrencyAmount, CurrencyCode, OrderId) VALUES 
            {string.Join(", ", values)}
            ";

        return await _database.ExecuteNonQueryAsync(query, parameters);
    }
}