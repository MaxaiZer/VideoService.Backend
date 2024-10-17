using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Data;

public class UnitOfWork: IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;

    private bool _disposed;

    public IVideoProcessingRequestRepository VideoProcessingRequests { get; }
    public IVideoRepository Videos { get; }

    public UnitOfWork(IConfiguration configuration)
    {
        _connection = new NpgsqlConnection(configuration["DB_CONNECTION_STRING"] ?? 
                                           throw new InvalidOperationException("DB connection string is missing in configuration"));
        VideoProcessingRequests = new VideoProcessingRequestRepository(_connection);
        Videos = new VideoRepository(_connection);
    }

    public void BeginTransaction()
    {
        if (_connection.State == ConnectionState.Closed)
        {
            _connection.Open();
        }

        _transaction = _connection.BeginTransaction();
    }

    public void CommitTransaction()
    {
        if (_transaction != null)
        {
            _transaction.Commit();
            _connection.Close();
        }
    }

    public void RollbackTransaction()
    {
        if (_transaction != null)
        {
            _transaction.Rollback();
            _connection.Close();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            _connection.Dispose();
            _transaction?.Dispose();
            _disposed = true;
        }
    }
}