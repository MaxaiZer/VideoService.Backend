using System.Linq.Expressions;

namespace Domain.Interfaces.Repositories
{
    public interface IRepositoryBase<T>
    {
        IQueryable<T> FindAll(bool trackChanges);
        Task<List<T>> FindByConditionAsync(Expression<Func<T, bool>> expression,
        bool trackChanges);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
