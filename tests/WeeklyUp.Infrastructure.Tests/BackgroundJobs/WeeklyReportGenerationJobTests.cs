using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Infrastructure.BackgroundJobs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Tests.BackgroundJobs;

public sealed class WeeklyReportGenerationJobTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<WeeklyReportGenerationJob> _logger = Substitute.For<ILogger<WeeklyReportGenerationJob>>();

    private WeeklyReportGenerationJob CreateJob() =>
        new(_mediator, _users, _dateTime, _logger);

    private static User CreateActiveUser()
    {
        var result = User.Create("user@test.com", "Test User", "My Business", BusinessType.Ecommerce);
        var user = result.Value;
        user.VerifyEmail();
        return user;
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsersForDay_ShouldNotSendAnyCommands()
    {
        // Arrange
        var job = CreateJob();
        _dateTime.Today.Returns(new DateOnly(2026, 2, 23)); // Monday
        _users.GetActiveUsersForReportAsync(Arg.Any<DayOfWeekPreference>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>().AsReadOnly());

        // Act
        await job.ExecuteAsync();

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsersExistForDay_ShouldSendCommandForEachUser()
    {
        // Arrange
        var job = CreateJob();
        _dateTime.Today.Returns(new DateOnly(2026, 2, 23)); // Monday

        var user1 = CreateActiveUser();
        var user2 = CreateActiveUser();
        var users = new List<User> { user1, user2 }.AsReadOnly();

        _users.GetActiveUsersForReportAsync(Arg.Any<DayOfWeekPreference>(), Arg.Any<CancellationToken>())
            .Returns(users);

        _mediator.Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<bool>(true));

        // Act
        await job.ExecuteAsync();

        // Assert
        await _mediator.Received(2).Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandFails_ShouldLogError()
    {
        // Arrange
        var job = CreateJob();
        _dateTime.Today.Returns(new DateOnly(2026, 2, 23)); // Monday

        var user = CreateActiveUser();
        _users.GetActiveUsersForReportAsync(Arg.Any<DayOfWeekPreference>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user }.AsReadOnly());

        _mediator.Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AppError.Failure("Report.GenerationFailed", "Erro ao gerar")));

        // Act
        await job.ExecuteAsync();

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
