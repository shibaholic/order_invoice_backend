using HospitalSupply.Entities;
using Npgsql;

namespace HospitalSupply.Repositories;

public interface IInvoiceRepository
{
    Task<List<InvoiceDto>> GetInvoiceDtosAsync();
    Task<int> CreateAsync(Invoice invoice);
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDatabase _database;
    public InvoiceRepository(IDatabase database)
    {
        _database = database;
    }

    public async Task<List<InvoiceDto>> GetInvoiceDtosAsync()
    {
        var query = @"SELECT Id, DateCreated, Scanned, Linked FROM Invoices ORDER BY DateCreated DESC";

        var result = await _database.ExecuteQueryAsync(query, reader => new InvoiceDto
        {
            Id = reader.GetGuid(0),
            DateCreated= reader.GetDateTime(1),
            Scanned = reader.GetBoolean(2),
            Linked = reader.GetBoolean(3),
        });

        return result;
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