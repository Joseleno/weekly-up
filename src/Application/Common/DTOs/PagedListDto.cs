namespace WeeklyUp.Application.Common.DTOs;

public sealed record PagedListDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);
