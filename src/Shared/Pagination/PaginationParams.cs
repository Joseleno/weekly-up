namespace WeeklyUp.Shared.Pagination;

public sealed record PaginationParams
{
    private const int MaxPageSize = 50;
    private const int DefaultPageSize = 10;

    public int PageNumber { get; init; } = 1;

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
