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
        
        private bool _disposed;
        
        public UnitOfWork(RepositoryContext context)
        {
            _context = context;
        }

        public IVideoRepository Videos => new VideoRepository(_context);
        public IVideoProcessingRequestRepository VideoProcessingRequests => new VideoProcessingRequestRepository(_context);

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress.");

            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction is in progress.");

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
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
                _context.Dispose();
                _transaction?.Dispose();
                _disposed = true;
            }
        }
    }
}
