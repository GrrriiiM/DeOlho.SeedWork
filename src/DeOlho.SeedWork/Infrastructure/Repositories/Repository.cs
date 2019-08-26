using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeOlho.SeedWork.Domain;
using DeOlho.SeedWork.Domain.Abstractions;
using DeOlho.SeedWork.Infrastructure.Data;

namespace DeOlho.SeedWork.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : Entity
    {

        DeOlhoDbContext _deOlhoDbContext;
        private readonly IQueryable<T> _query;
        public Repository(
            DeOlhoDbContext deOlhoDbContext)
        {
            _deOlhoDbContext = deOlhoDbContext;
            _query = _deOlhoDbContext.Set<T>();
        }

        public IUnitOfWork UnityOfWork => _deOlhoDbContext;
        public IQueryable<T> Query => _query;

        public T Add(T entity)
        {
            return _deOlhoDbContext.Add(entity).Entity;
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _deOlhoDbContext.AddRange(entities);
        }

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await _deOlhoDbContext.AddAsync(entity, cancellationToken)).Entity;
        }
        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _deOlhoDbContext.AddRangeAsync(entities, cancellationToken);
        }

        public T Find(object key)
        {
            return (T)_deOlhoDbContext.Find(typeof(T), key);
        }

        public async Task<T> FindAsync(object key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (T) await _deOlhoDbContext.FindAsync(typeof(T), key, cancellationToken);
        }

        public void Remove(T entity)
        {
            _deOlhoDbContext.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _deOlhoDbContext.RemoveRange(entities);
        }

        public T Update(T entity)
        {
            return _deOlhoDbContext.Update(entity).Entity;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            _deOlhoDbContext.UpdateRange(entities);
        }
    }
}