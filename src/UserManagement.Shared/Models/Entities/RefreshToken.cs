using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserManagement.Shared.Models.Entities;

/// <summary>
/// Refresh token entity representing an issued token for token renewal.
/// Stored in MongoDB for tracking and revocation.
/// </summary>
[BsonCollection("refreshTokens")]
public class RefreshToken
{
    /// <summary>
    /// Unique MongoDB object ID for the token record.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// ID of the user who owns this refresh token.
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The actual refresh token value (cryptographically random).
    /// Should be stored hashed in production, but for simplicity stored in plain text.
    /// </summary>
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this refresh token will expire (UTC).
    /// </summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if this token has been revoked.
    /// Revoked tokens cannot be used even if not expired.
    /// </summary>
    [BsonElement("isRevoked")]
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Timestamp when the token was revoked. Null if not revoked.
    /// </summary>
    [BsonElement("revokedAt")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Timestamp when this token was created (UTC).
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the token was issued. For tracking.
    /// </summary>
    [BsonElement("ipAddress")]
    [BsonIgnoreIfNull]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent from which the token was issued. For tracking.
    /// </summary>
    [BsonElement("userAgent")]
    [BsonIgnoreIfNull]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Indicates if this token is valid (not revoked and not expired).
    /// </summary>
    public bool IsValid => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}
