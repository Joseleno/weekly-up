using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Infrastructure.BackgroundJobs;

namespace WeeklyUp.Infrastructure.Tests.BackgroundJobs;

public sealed class TokenRefreshJobTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ILogger<TokenRefreshJob> _logger = Substitute.For<ILogger<TokenRefreshJob>>();
    private readonly IIntegrationRepository _integrations = Substitute.For<IIntegrationRepository>();

    public TokenRefreshJobTests()
    {
        _uow.Integrations.Returns(_integrations);
    }

    private TokenRefreshJob CreateJob() => new(_uow, _logger);

    private static Integration CreateConnectedIntegration()
    {
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns("encrypted");

        var userResult = User.Create("owner@test.com", "Owner", "Business", BusinessType.Ecommerce, verificationToken: "test-token");
        var user = userResult.Value;
        user.AddIntegration(
            IntegrationProvider.GoogleAnalytics4,
            "access-token",
            "refresh-token",
            "account-1",
            null,
            encryptor);

        return user.Integrations.First();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExpiredTokens_ShouldNotCallSaveChanges()
    {
        // Arrange
        var job = CreateJob();
        _integrations.GetActiveWithExpiredTokensAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Integration>().AsReadOnly());

        // Act
        await job.ExecuteAsync();

        // Assert
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExpiredTokensExist_ShouldMarkThemExpiredAndSave()
    {
        // Arrange
        var job = CreateJob();
        var integration = CreateConnectedIntegration();
        var expiredList = new List<Integration> { integration }.AsReadOnly();

        _integrations.GetActiveWithExpiredTokensAsync(Arg.Any<CancellationToken>())
            .Returns(expiredList);

        // Act
        await job.ExecuteAsync();

        // Assert
        integration.Status.Should().Be(IntegrationStatus.Error);
        integration.LastError.Should().NotBeNullOrEmpty();

        _integrations.Received(1).Update(integration);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
