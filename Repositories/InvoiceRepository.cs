using HospitalSupply.Entities;
using Npgsql;

namespace HospitalSupply.Repositories;

public interface IInvoiceRepository
{
    Task<List<InvoiceDto>> GetInvoiceDtosAsync();
    Task<Invoice?> GetInvoiceAsync(Guid id);
    Task<int> CreateAsync(Invoice invoice);
    Task<int> UpdateAsync(Invoice invoice);
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

    public async Task<Invoice?> GetInvoiceAsync(Guid id)
    {
        var query = @"SELECT * FROM Invoices WHERE Id = @Id";

        var parameters = new Dictionary<string, object>
        {
            { "@Id", id }
        };

        var result = await _database.ExecuteQueryAsync<Invoice>(query, reader =>
        {
            return new Invoice
            {
                Id = reader.GetGuid(0),
                FileName = reader.GetString(1),
                FileData = (byte[])reader["FileData"],
                ContentType = reader.GetString(3),
                DateCreated = reader.GetDateTime(4),
                Scanned = reader.GetBoolean(5),
                Linked = reader.GetBoolean(6),
            };
        }, parameters);

        return result.FirstOrDefault();
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

    public async Task<int> UpdateAsync(Invoice invoice)
    {
        var query = @"
            UPDATE Invoices
            SET FileName = @FileName, 
                FileData = @FileData, 
                ContentType = @ContentType, 
                DateCreated = @DateCreated, 
                Scanned = @Scanned, 
                Linked = @Linked
            WHERE Id = @Id;
        ";
        
        var parameters = new Dictionary<string, object>
        {
            { "@Id", invoice.Id },
            { "@FileName", invoice.FileName },
            { "@FileData", invoice.FileData },
            { "@ContentType", invoice.ContentType },
            { "@DateCreated", invoice.DateCreated },
            { "@Scanned", invoice.Scanned },
            { "@Linked", invoice.Linked }
        };
        
        return await _database.ExecuteNonQueryAsync(query, parameters);
    }
}