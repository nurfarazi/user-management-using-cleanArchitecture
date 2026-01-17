namespace UserManagement.Repository.Configuration;

/// <summary>
/// Configuration settings for MongoDB connectivity.
/// These settings are typically loaded from appsettings.json and bound using the Options pattern.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// The MongoDB connection string.
    /// Format: mongodb://[username:password@]host[:port][/[defaultauthdb]]
    /// Example: mongodb://localhost:27017
    /// Example with auth: mongodb://user:pass@host:27017/admin
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The name of the MongoDB database to connect to.
    /// This database will be created if it doesn't exist.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of connections in the connection pool.
    /// Default MongoDB driver uses 100. Increase for high-concurrency scenarios.
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Server selection timeout in milliseconds.
    /// Time to wait when trying to select a server for an operation.
    /// </summary>
    public int ServerSelectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Enables automatic retries of failed write operations.
    /// Requires a replica set deployment for MongoDB 4.0+
    /// </summary>
    public bool RetryWrites { get; set; } = true;
}
