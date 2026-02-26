namespace WeeklyUp.Shared.Pagination;

public sealed record PaginationParams
{
    private const int MaxPageSize = 50;
    private const int DefaultPageSize = 10;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public PaginationParams() { }

    public PaginationParams(int pageNumber, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0);
        PageNumber = pageNumber;
        PageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }
}
