using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Queries.GetReportDetail;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Reports.Queries;

public sealed class GetReportDetailQueryHandlerTests
{
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IApplicationCacheService _cache = Substitute.For<IApplicationCacheService>();
    private readonly GetReportDetailQueryHandler _sut;

    public GetReportDetailQueryHandlerTests()
    {
        _sut = new GetReportDetailQueryHandler(_reports, _cache);
    }

    private static Report CreateReport(Guid userId)
    {
        var weekRange = DateRange.Create(
            new DateOnly(2025, 2, 17),
            new DateOnly(2025, 2, 23)).Value;
        return Report.Create(userId, weekRange);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var query = new GetReportDetailQuery(userId, reportId);
        var report = CreateReport(userId);
        var cachedDto = new ReportDetailDto(reportId, "2025-02-17 a 2025-02-23", "Pending", null, null, null, DateTimeOffset.UtcNow);
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);
        _cache.GetAsync<ReportDetailDto>(CacheKeys.Report(reportId), Arg.Any<CancellationToken>())
            .Returns(cachedDto);

        // Act
        Result<ReportDetailDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedDto);
    }

    [Fact]
    public async Task Handle_WhenReportNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var query = new GetReportDetailQuery(userId, reportId);
        _cache.GetAsync<ReportDetailDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportDetailDto?)null);
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns((Report?)null);

        // Act
        Result<ReportDetailDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("Report.NotFound");
    }

    [Fact]
    public async Task Handle_WhenReportBelongsToDifferentUser_ShouldReturnNotFoundError()
    {
        // Arrange
        var requestingUserId = Guid.NewGuid();
        var reportOwnerId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var query = new GetReportDetailQuery(requestingUserId, reportId);
        var report = CreateReport(reportOwnerId);
        _cache.GetAsync<ReportDetailDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportDetailDto?)null);
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);

        // Act
        Result<ReportDetailDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.NotFound);
        result.Error.Code.Should().Be("Report.NotFound");
    }

    [Fact]
    public async Task Handle_WhenReportFound_ShouldReturnDetailDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var query = new GetReportDetailQuery(userId, reportId);
        var report = CreateReport(userId);
        _cache.GetAsync<ReportDetailDto>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ReportDetailDto?)null);
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns(report);

        // Act
        Result<ReportDetailDto> result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Pending");
        await _cache.Received(1).SetAsync(
            CacheKeys.Report(reportId),
            Arg.Any<ReportDetailDto>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }
}
