using System.Linq.Expressions;
using Domain.Interfaces.Repositories;
using CoreService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Infrastructure.Data.Repositories
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected RepositoryContext context;
        public RepositoryBase(RepositoryContext repositoryContext)
        {
            context = repositoryContext;
        }

        public IQueryable<T> FindAll(bool trackChanges) =>
            trackChanges ? context.Set<T>() : context.Set<T>().AsNoTracking();

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges) =>
            trackChanges ? context.Set<T>().Where(expression) : context.Set<T>().Where(expression).AsNoTracking();

        public void Create(T entity) => context.Set<T>().Add(entity);
        public void Update(T entity) => context.Set<T>().Update(entity);
        public void Delete(T entity) => context.Set<T>().Remove(entity);
    }

}
