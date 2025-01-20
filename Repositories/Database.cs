using System.Data;
using Npgsql;

namespace HospitalSupply.Repositories;

public interface IDatabase
{
    NpgsqlConnection GetConnection();
    NpgsqlTransaction BeginTransaction();
    Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters = null);
    Task<T> ExecuteScalarAsync<T>(string query, Dictionary<string, object>? parameters = null);
    Task<List<T>> ExecuteQueryAsync<T>(string query, Func<IDataRecord, T> map, Dictionary<string, object>? parameters = null);
}

public class Database : IDatabase
{
    private string connectionString;
    private NpgsqlConnection? _connection = null;
    
    public Database(string connectionString)
    {
        this.connectionString = connectionString;
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }

    public NpgsqlConnection GetConnection()
    {
        if (CheckConnection())
        {
            return _connection!;
        }
        throw new InvalidOperationException("No connection available due to connection error.");
    }

    public NpgsqlTransaction BeginTransaction()
    {
        if (CheckConnection())
        { 
            return _connection!.BeginTransaction();
        }
        throw new InvalidOperationException("No transaction available due to connection error.");
    }

    private bool CheckConnection()
    {
        return _connection is not null && _connection.State == ConnectionState.Open;
    }
    
    public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters = null)
    {
        using var command = new NpgsqlCommand(query, _connection);

        AddParameters(command, parameters);
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<T> ExecuteScalarAsync<T>(string query, Dictionary<string, object>? parameters = null)
    {
        using var command = new NpgsqlCommand(query, _connection);

        AddParameters(command, parameters);
        var result = await command.ExecuteScalarAsync();

        return result == null || result == DBNull.Value ? default : (T)result;
    }
    
    public async Task<List<T>> ExecuteQueryAsync<T>(string query, Func<IDataRecord, T> map, Dictionary<string, object>? parameters = null)
    {
        var results = new List<T>();

        using var command = new NpgsqlCommand(query, _connection);

        AddParameters(command, parameters);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(map(reader));
        }

        return results;
    }
    
    
    private void AddParameters(NpgsqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters == null) return;

        foreach (var param in parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }
    }
}