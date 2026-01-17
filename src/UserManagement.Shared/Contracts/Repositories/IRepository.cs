using System.Linq.Expressions;

namespace UserManagement.Shared.Contracts.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations.
/// All repositories extend this interface to provide consistent data access patterns.
/// This interface abstracts persistence details from upper layers.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository manages.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <returns>The entity if found; null otherwise.</returns>
    Task<TEntity?> GetByIdAsync(string id);

    /// <summary>
    /// Retrieves all entities of the given type.
    /// Use with caution on large datasets; consider pagination.
    /// </summary>
    /// <returns>All entities of type TEntity.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity with any generated fields populated (e.g., ID).</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to update.</param>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>True if the update was successful; false if the entity was not found.</returns>
    Task<bool> UpdateAsync(string id, TEntity entity);

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <returns>True if the deletion was successful; false if the entity was not found.</returns>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Checks if an entity matching the predicate exists.
    /// </summary>
    /// <param name="predicate">The condition to check for entity existence.</param>
    /// <returns>True if an entity matching the predicate exists; false otherwise.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Finds the first entity matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>The first matching entity; null if no entity matches.</returns>
    Task<TEntity?> FindOneAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Finds all entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>All entities matching the predicate.</returns>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
}
