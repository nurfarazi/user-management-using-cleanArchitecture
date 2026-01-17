using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using UserManagement.Shared.Contracts.Repositories;
using UserManagement.Shared.Models.Entities;

namespace UserManagement.Repository;

/// <summary>
/// Generic MongoDB repository implementation.
/// Provides common CRUD operations for any entity type.
/// All specific repositories should inherit from this class to get standard functionality.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository manages.</typeparam>
public abstract class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// MongoDB database instance.
    /// </summary>
    protected readonly IMongoDatabase Database;

    /// <summary>
    /// Logger for data access operations.
    /// </summary>
    protected readonly ILogger<BaseRepository<TEntity>> Logger;

    /// <summary>
    /// MongoDB collection for this entity type.
    /// </summary>
    protected IMongoCollection<TEntity> Collection { get; set; }

    /// <summary>
    /// Initializes a new instance of the BaseRepository class.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    /// <param name="logger">Logger for data operations.</param>
    protected BaseRepository(IMongoDatabase database, ILogger<BaseRepository<TEntity>> logger)
    {
        Database = database;
        Logger = logger;

        // Get collection name from entity type or BsonCollection attribute
        var collectionName = GetCollectionName();
        Collection = database.GetCollection<TEntity>(collectionName);

        Logger.LogInformation("BaseRepository initialized for entity type {EntityType} with collection {CollectionName}",
            typeof(TEntity).Name, collectionName);
    }

    /// <summary>
    /// Gets the MongoDB collection name for this entity.
    /// Uses BsonCollectionAttribute if present; otherwise uses plural form of entity name.
    /// </summary>
    /// <returns>The collection name.</returns>
    protected virtual string GetCollectionName()
    {
        // Check for BsonCollectionAttribute
        var attribute = typeof(TEntity)
            .GetCustomAttributes(false)
            .OfType<BsonCollectionAttribute>()
            .FirstOrDefault();

        if (attribute != null)
            return attribute.CollectionName;

        // Default: plural form of entity name (User -> users)
        return $"{typeof(TEntity).Name.ToLower()}s";
    }

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier (MongoDB ObjectId as string).</param>
    /// <returns>The entity if found; null otherwise.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                Logger.LogWarning("Invalid MongoDB ObjectId format: {Id}", id);
                return null;
            }

            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var entity = await Collection.Find(filter).FirstOrDefaultAsync();

            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entity by ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all entities of the given type.
    /// Use with caution on large datasets; consider pagination.
    /// </summary>
    /// <returns>All entities of type TEntity.</returns>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        try
        {
            var entities = await Collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync();
            return entities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <returns>The added entity with ID populated.</returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        try
        {
            await Collection.InsertOneAsync(entity);
            Logger.LogInformation("Entity of type {EntityType} inserted successfully", typeof(TEntity).Name);
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to update.</param>
    /// <param name="entity">The entity with updated values.</param>
    /// <returns>True if the update was successful; false if the entity was not found.</returns>
    public virtual async Task<bool> UpdateAsync(string id, TEntity entity)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                Logger.LogWarning("Invalid MongoDB ObjectId format during update: {Id}", id);
                return false;
            }

            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var result = await Collection.ReplaceOneAsync(filter, entity);

            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating entity of type {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to delete.</param>
    /// <returns>True if the deletion was successful; false if the entity was not found.</returns>
    public virtual async Task<bool> DeleteAsync(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                Logger.LogWarning("Invalid MongoDB ObjectId format during delete: {Id}", id);
                return false;
            }

            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var result = await Collection.DeleteOneAsync(filter);

            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entity of type {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Checks if an entity matching the predicate exists.
    /// </summary>
    /// <param name="predicate">The condition to check for entity existence.</param>
    /// <returns>True if an entity matching the predicate exists; false otherwise.</returns>
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var exists = await Collection.Find(filter).AnyAsync();
            return exists;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking existence of entity with predicate");
            throw;
        }
    }

    /// <summary>
    /// Finds the first entity matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>The first matching entity; null if no entity matches.</returns>
    public virtual async Task<TEntity?> FindOneAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var entity = await Collection.Find(filter).FirstOrDefaultAsync();
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding single entity with predicate");
            throw;
        }
    }

    /// <summary>
    /// Finds all entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to filter entities.</param>
    /// <returns>All entities matching the predicate.</returns>
    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var entities = await Collection.Find(filter).ToListAsync();
            return entities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error finding entities with predicate");
            throw;
        }
    }
}
