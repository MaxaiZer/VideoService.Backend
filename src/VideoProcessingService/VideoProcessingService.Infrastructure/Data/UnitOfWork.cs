using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using VideoProcessingService.Core.Interfaces;

namespace VideoProcessingService.Infrastructure.Data;

public class UnitOfWork: IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;

    public IVideoProcessingRequestRepository VideoProcessingRequests { get; }
    public IVideoRepository Videos { get; }

    public UnitOfWork(IConfiguration configuration)
    {
        _connection = new NpgsqlConnection(configuration["DB_CONNECTION_STRING"])
                                           ?? throw new Exception("Can't find a connection string");
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
        _connection.Dispose();
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}