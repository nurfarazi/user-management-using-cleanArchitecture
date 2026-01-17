using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UserManagement.Repository.Configuration;
using UserManagement.Repository.Implementations;
using UserManagement.Shared.Contracts.Repositories;

namespace UserManagement.Repository;

/// <summary>
/// Dependency injection extension methods for the Repository layer.
/// Registers MongoDB client, database, and all repository implementations.
/// </summary>
public static class RepositoryDependencyInjection
{
    /// <summary>
    /// Adds all repository layer services to the DI container.
    /// Configures MongoDB connectivity and registers repositories.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configuration">The application configuration containing MongoDB settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Get and validate MongoDB settings from configuration
        var mongoDbSettingsSection = configuration.GetSection(nameof(MongoDbSettings));
        var mongoDbSettings = mongoDbSettingsSection.Get<MongoDbSettings>();

        if (mongoDbSettings == null)
            throw new InvalidOperationException(
                $"MongoDB settings not configured. Ensure {nameof(MongoDbSettings)} section exists in appsettings.json");

        // 2. Register MongoDB Client as Singleton
        // The MongoDB driver manages connection pooling internally and is thread-safe
        services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoUrl = MongoUrl.Create(mongoDbSettings.ConnectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

            // Configure connection pool size
            clientSettings.MaxConnectionPoolSize = mongoDbSettings.MaxConnectionPoolSize;
            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(mongoDbSettings.ServerSelectionTimeoutMs);
            clientSettings.RetryWrites = mongoDbSettings.RetryWrites;

            return new MongoClient(clientSettings);
        });

        // 3. Register MongoDB Database as Singleton
        // Database is a lightweight reference and can safely be singleton
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(mongoDbSettings.DatabaseName);
        });

        // 4. Register Repositories as Scoped
        // Repositories should be scoped to HTTP request in web context
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }
}
