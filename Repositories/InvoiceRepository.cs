using HospitalSupply.Entities;

namespace HospitalSupply.Repositories;

public interface IInvoiceRepository
{
    Task<int> CreateAsync(Invoice invoice);
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDatabase _database;
    public InvoiceRepository(IDatabase database)
    {
        _database = database;
    }
    
    public async Task<int> CreateAsync(Invoice invoice)
    {
        var query = "INSERT INTO Invoices (Id, FileName, FileData, ContentType) VALUES (@Id, @FileName, @FileData, @ContentType)";
        var parameters = new Dictionary<string, object>
        {
            { "@Id", invoice.Id },
            { "@FileName", invoice.FileName },
            { "@FileData", invoice.FileData },
            { "@ContentType", invoice.ContentType },
        };

        return await _database.ExecuteNonQueryAsync(query, parameters);
    }
}