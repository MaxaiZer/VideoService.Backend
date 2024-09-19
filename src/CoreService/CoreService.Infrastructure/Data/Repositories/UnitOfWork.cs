using Domain.Interfaces.Repositories;
using CoreService.Application.Interfaces;
using CoreService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace CoreService.Infrastructure.Data.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly RepositoryContext _context;
        private IDbContextTransaction? _transaction;
        
        public UnitOfWork(RepositoryContext context)
        {
            this._context = context;
        }

        public IVideoRepository Videos => new VideoRepository(_context);
        public IVideoProcessingRequestRepository VideoProcessingRequests => new VideoProcessingRequestRepository(_context);

        public void BeginTransaction()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _transaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress.");

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        public void RollbackTransaction()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress.");

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }
        
        public void Save()
        {
            _context.SaveChanges();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
