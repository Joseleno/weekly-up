using FluentAssertions;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Reports.Commands;

public sealed class GenerateWeeklyReportCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IReportJobScheduler _jobScheduler = Substitute.For<IReportJobScheduler>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly GenerateWeeklyReportCommandHandler _sut;

    public GenerateWeeklyReportCommandHandlerTests()
    {
        _uow.Reports.Returns(_reports);
        _dateTime.Today.Returns(new DateOnly(2025, 2, 19)); // Wednesday
        _sut = new GenerateWeeklyReportCommandHandler(_uow, _jobScheduler, _dateTime);
    }

    [Fact]
    public async Task Handle_WhenReportAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new GenerateWeeklyReportCommand(userId);
        var weekStart = new DateOnly(2025, 2, 17); // Monday
        var existingReport = Report.Create(
            userId,
            DateRange.Create(weekStart, weekStart.AddDays(6)).Value);

        _reports.GetByUserAndWeekAsync(userId, weekStart, Arg.Any<CancellationToken>())
            .Returns(existingReport);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(AppErrorType.Conflict);
        result.Error.Code.Should().Be("Report.AlreadyExists");
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoReportExists_ShouldCreateReportAndScheduleJob()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new GenerateWeeklyReportCommand(userId);
        var weekStart = new DateOnly(2025, 2, 17); // Monday

        _reports.GetByUserAndWeekAsync(userId, weekStart, Arg.Any<CancellationToken>())
            .Returns((Report?)null);

        // Act
        Result<bool> result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await _reports.Received(1).AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.Received(1).ScheduleReportSending(Arg.Any<Guid>());
    }
}
