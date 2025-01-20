using Npgsql;

namespace HospitalSupply.Repositories;

public interface IUnitOfWork
{
    void BeginTransaction();
    void Commit();
}

public class UnitOfWork : IDisposable, IUnitOfWork
{
    private NpgsqlTransaction? _transaction = null;
    private readonly IDatabase _database;

    public UnitOfWork(IDatabase database)
    {
        _database = database;
    }

    public void BeginTransaction()
    {
        if(_transaction != null) throw new InvalidOperationException("The transaction has already been started.");
        _transaction = _database.BeginTransaction();
    }

    public void Commit()
    {
        if(_transaction == null) throw new InvalidOperationException("No transaction started.");
        _transaction.Commit();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}