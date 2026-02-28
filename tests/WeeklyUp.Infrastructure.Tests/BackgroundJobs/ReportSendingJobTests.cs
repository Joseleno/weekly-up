using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Infrastructure.BackgroundJobs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Tests.BackgroundJobs;

public sealed class ReportSendingJobTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IWhatsAppSender _whatsAppSender = Substitute.For<IWhatsAppSender>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IApplicationCacheService _cache = Substitute.For<IApplicationCacheService>();
    private readonly ILogger<ReportSendingJob> _logger = Substitute.For<ILogger<ReportSendingJob>>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    public ReportSendingJobTests()
    {
        _uow.Reports.Returns(_reports);
        _uow.Users.Returns(_users);
    }

    private ReportSendingJob CreateJob() => new(_emailSender, _whatsAppSender, _uow, _cache, _logger);

    private static Report CreateReport(Guid? userId = null)
    {
        var weekRange = DateRange.Create(
            new DateOnly(2026, 2, 16),
            new DateOnly(2026, 2, 22)).Value;
        return Report.Create(userId ?? Guid.NewGuid(), weekRange);
    }

    private static User CreateUserWithPlan(PlanType plan = PlanType.Free)
    {
        var result = User.Create("user@test.com", "Test User", "My Business", BusinessType.Ecommerce, verificationToken: "test-token");
        var user = result.Value;

        if (plan >= PlanType.Pro)
        {
            user.UpgradePlan(PlanType.Pro);
        }

        if (plan >= PlanType.Business)
        {
            user.UpgradePlan(PlanType.Business);
        }

        return user;
    }

    [Fact]
    public async Task ExecuteAsync_WhenReportNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var job = CreateJob();
        var reportId = Guid.NewGuid();
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns((Report?)null);

        // Act
        await job.ExecuteAsync(reportId);

        // Assert
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var job = CreateJob();
        var report = CreateReport();
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(report.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailFails_ShouldLogErrorAndNotSave()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUserWithPlan();
        var report = CreateReport(user.Id);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _emailSender.SendWeeklyReportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Report>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AppError.Failure("Email.SendFailed", "SMTP error")));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailSucceeds_ShouldMarkReportSentAndSave()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUserWithPlan();
        var report = CreateReport(user.Id);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _emailSender.SendWeeklyReportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Report>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<bool>(true));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Sent);
        report.EmailSentAt.Should().NotBeNull();

        _reports.Received(1).Update(report);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenWhatsAppEnabled_ShouldSendWhatsApp()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUserWithPlan(PlanType.Business);
        user.SetPhoneNumber("+5511999990000");
        var report = CreateReport(user.Id);

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _emailSender.SendWeeklyReportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Report>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<bool>(true));
        _whatsAppSender.SendWeeklyReportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Report>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<bool>(true));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.WhatsAppSentAt.Should().NotBeNull();
        await _whatsAppSender.Received(1).SendWeeklyReportAsync(
            user.PhoneNumber!, Arg.Any<string>(), report, Arg.Any<CancellationToken>());
    }
}
