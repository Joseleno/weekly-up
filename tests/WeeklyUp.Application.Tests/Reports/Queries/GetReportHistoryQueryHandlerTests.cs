using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Reports.Queries.GetReportHistory;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Reports.Queries;

public sealed class GetReportHistoryQueryHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly GetReportHistoryQueryHandler _sut;

    public GetReportHistoryQueryHandlerTests()
    {
        _sut = new GetReportHistoryQueryHandler(_reports);
    }

    private static Report CreateReport(Guid userId, int weekOffsetFromNow = 0)
    {
        var monday = new DateOnly(2025, 2, 17).AddDays(weekOffsetFromNow * 7);
        var weekRange = DateRange.Create(monday, monday.AddDays(6)).Value;
        return Report.Create(userId, weekRange);
    }

    [Fact]
    public async Task Handle_WhenNoReports_ShouldReturnEmptyPagedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetReportHistoryQuery(userId, Page: 1, PageSize: 10);
        _reports.GetPagedHistoryAsync(userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Report>() as IReadOnlyList<Report>, 0));

        // Act
        Result<PagedListDto<ReportSummaryDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenReportsExist_ShouldReturnPagedList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetReportHistoryQuery(userId, Page: 1, PageSize: 5);
        var pageReports = Enumerable.Range(0, 5)
            .Select(i => CreateReport(userId, i))
            .ToList()
            .AsReadOnly() as IReadOnlyList<Report>;
        _reports.GetPagedHistoryAsync(userId, 1, 5, Arg.Any<CancellationToken>())
            .Returns((pageReports!, 8));

        // Act
        Result<PagedListDto<ReportSummaryDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(8);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPageExceedsTotal_ShouldReturnEmptyItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetReportHistoryQuery(userId, Page: 5, PageSize: 10);
        _reports.GetPagedHistoryAsync(userId, 5, 10, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Report>() as IReadOnlyList<Report>, 3));

        // Act
        Result<PagedListDto<ReportSummaryDto>> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(3);
    }
}
