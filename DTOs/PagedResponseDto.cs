namespace PaymentApi.DTOs;

/// <summary>Generic paginated response envelope.</summary>
public class PagedResponseDto<T>
{
    /// <summary>Current page number (1-based).</summary>
    public int Page { get; init; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of items matching the filter.</summary>
    public int TotalCount { get; init; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Items on the current page.</summary>
    public IEnumerable<T> Data { get; init; } = [];
}
