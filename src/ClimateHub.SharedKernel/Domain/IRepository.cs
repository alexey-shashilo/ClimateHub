using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.SharedKernel.Domain;

public interface IRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : struct
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}