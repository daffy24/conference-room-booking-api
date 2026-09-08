namespace ConferenceBooking.Core.Application.Models;

/// <summary>
/// A bounded page of matching resources.
/// </summary>
/// <param name="Items">The requested page.</param>
/// <param name="TotalCount">The number of matching resources at query time.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The maximum number of items in a page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
