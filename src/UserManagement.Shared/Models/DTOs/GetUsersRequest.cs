namespace UserManagement.Shared.Models.DTOs;

/// <summary>
/// Request model for retrieving a paginated list of users with filtering and sorting.
/// </summary>
public class GetUsersRequest
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 10, max 100.
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Search term to filter users by name or email.
    /// Optional.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Field to sort by. Valid values: "email", "firstName", "lastName", "createdAt", "role".
    /// Defaults to "createdAt".
    /// </summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>
    /// Sort order. Valid values: "asc" (ascending) or "desc" (descending).
    /// Defaults to "desc".
    /// </summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>
    /// Filter by user role. Optional.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Filter users created after this date. Optional.
    /// </summary>
    public DateTime? CreatedAfter { get; set; }

    /// <summary>
    /// Filter users created before this date. Optional.
    /// </summary>
    public DateTime? CreatedBefore { get; set; }

    /// <summary>
    /// Include deleted users in results. Defaults to false.
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
