using FluentAssertions;
using NSubstitute;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.Entities;

public sealed class UserTests
{
    private static ITokenEncryptor CreateEncryptor()
    {
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns(x => $"enc_{x.Arg<string>()}");
        return encryptor;
    }

    // ── Create ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsSuccess()
    {
        // Act
        var result = User.Create("user@test.com", "João Silva", "Minha Loja", BusinessType.Ecommerce);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Value.Should().Be("user@test.com");
        result.Value.Name.Should().Be("João Silva");
        result.Value.Plan.Should().Be(PlanType.Free);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void Create_InvalidEmail_ReturnsFailure()
    {
        // Act
        var result = User.Create("nao-e-email", "João", "Loja", BusinessType.Ecommerce);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidFormat");
    }

    [Fact]
    public void Create_EmptyName_ReturnsFailure()
    {
        // Act
        var result = User.Create("user@test.com", "   ", "Loja", BusinessType.Ecommerce);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NameEmpty");
    }

    [Fact]
    public void Create_InvalidBusinessName_ReturnsFailure()
    {
        // Act
        var result = User.Create("user@test.com", "João", "A", BusinessType.Ecommerce);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BusinessName.TooShort");
    }

    [Fact]
    public void Create_RaisesUserRegisteredEvent()
    {
        // Act
        var result = User.Create("user@test.com", "João", "Minha Loja", BusinessType.Ecommerce);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public void Create_WithExternalAuthId_SetsExternalAuthId()
    {
        // Act
        var result = User.Create("user@test.com", "João", "Minha Loja", BusinessType.Ecommerce, "google|123");

        // Assert
        result.Value.ExternalAuthId.Should().Be("google|123");
    }

    // ── AddIntegration ──────────────────────────────────────

    [Fact]
    public void AddIntegration_FreePlan_FirstIntegration_Succeeds()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.ClearDomainEvents();
        var encryptor = CreateEncryptor();

        // Act
        var result = user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "token", "refresh", "acc123", "prop123", encryptor);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Integrations.Should().HaveCount(1);
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<IntegrationConnectedEvent>();
    }

    [Fact]
    public void AddIntegration_FreePlan_SecondIntegration_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        var encryptor = CreateEncryptor();
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);

        // Act
        var result = user.AddIntegration(IntegrationProvider.Stripe, "t2", "r2", "acc2", null, encryptor);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.IntegrationLimitReached");
    }

    [Fact]
    public void AddIntegration_ProPlan_UpToThreeIntegrations_Succeeds()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);
        var encryptor = CreateEncryptor();

        // Act
        var r1 = user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);
        var r2 = user.AddIntegration(IntegrationProvider.Stripe, "t2", "r2", "acc2", null, encryptor);
        var r3 = user.AddIntegration(IntegrationProvider.Manual, "t3", "r3", "acc3", null, encryptor);

        // Assert
        r1.IsSuccess.Should().BeTrue();
        r2.IsSuccess.Should().BeTrue();
        r3.IsSuccess.Should().BeTrue();
        user.Integrations.Should().HaveCount(3);
    }

    [Fact]
    public void AddIntegration_DuplicateProvider_ReturnsConflict()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);
        var encryptor = CreateEncryptor();
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);

        // Act
        var result = user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t2", "r2", "acc2", null, encryptor);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.IntegrationAlreadyConnected");
    }

    // ── RemoveIntegration ───────────────────────────────────

    [Fact]
    public void RemoveIntegration_Existing_DisconnectsSuccessfully()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        var encryptor = CreateEncryptor();
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);
        user.ClearDomainEvents();

        // Act
        var result = user.RemoveIntegration(IntegrationProvider.GoogleAnalytics4);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<IntegrationDisconnectedEvent>();
    }

    [Fact]
    public void RemoveIntegration_NotFound_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act
        var result = user.RemoveIntegration(IntegrationProvider.Stripe);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.IntegrationNotFound");
    }

    // ── UpgradePlan ─────────────────────────────────────────

    [Fact]
    public void UpgradePlan_FreeToProSucceeds()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.ClearDomainEvents();

        // Act
        var result = user.UpgradePlan(PlanType.Pro);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Plan.Should().Be(PlanType.Pro);
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserPlanUpgradedEvent>();
    }

    [Fact]
    public void UpgradePlan_ProToFree_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);

        // Act
        var result = user.UpgradePlan(PlanType.Free);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidPlanUpgrade");
    }

    [Fact]
    public void UpgradePlan_SamePlan_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act
        var result = user.UpgradePlan(PlanType.Free);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ── VerifyEmail ─────────────────────────────────────────

    [Fact]
    public void VerifyEmail_SetsIsEmailVerifiedAndRaisesEvent()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.ClearDomainEvents();

        // Act
        user.VerifyEmail();

        // Assert
        user.IsEmailVerified.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserEmailVerifiedEvent>();
    }

    // ── Capability checks ───────────────────────────────────

    [Fact]
    public void CanAccessDemographics_Free_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act & Assert
        user.CanAccessDemographics().Should().BeFalse();
    }

    [Fact]
    public void CanAccessDemographics_Pro_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessDemographics().Should().BeTrue();
    }

    [Fact]
    public void CanAccessWhatsApp_ProPlan_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessWhatsApp().Should().BeFalse();
    }

    [Fact]
    public void CanAccessWhatsApp_BusinessPlan_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);
        user.UpgradePlan(PlanType.Business);

        // Act & Assert
        user.CanAccessWhatsApp().Should().BeTrue();
    }

    [Fact]
    public void CanAccessInsights_FreePlan_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act & Assert
        user.CanAccessInsights().Should().BeFalse();
    }

    [Fact]
    public void CanAccessInsights_ProPlan_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessInsights().Should().BeTrue();
    }

    // ── SetPhoneNumber ───────────────────────────────────────

    [Fact]
    public void SetPhoneNumber_ValidNumber_SetsPhoneNumber()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act
        user.SetPhoneNumber("+5511999999999");

        // Assert
        user.PhoneNumber.Should().Be("+5511999999999");
    }

    [Fact]
    public void SetPhoneNumber_Null_ClearsPhoneNumber()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        user.SetPhoneNumber("+5511999999999");

        // Act
        user.SetPhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    // ── UpdateProfile ───────────────────────────────────────

    [Fact]
    public void UpdateProfile_ValidData_UpdatesFields()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja Antiga", BusinessType.Ecommerce).Value;

        // Act
        var result = user.UpdateProfile("Maria", "Loja Nova", BusinessType.Services);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Name.Should().Be("Maria");
        user.BusinessType.Should().Be(BusinessType.Services);
    }

    [Fact]
    public void UpdateProfile_EmptyName_ReturnsFailure()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act
        var result = user.UpdateProfile("  ", "Loja", BusinessType.Ecommerce);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NameEmpty");
    }

    // ── Deactivate ──────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }

    // ── AddIntegration re-add after disconnect ──────────────

    [Fact]
    public void AddIntegration_AfterDisconnect_AllowsReconnect()
    {
        // Arrange
        var user = User.Create("user@test.com", "João", "Loja", BusinessType.Ecommerce).Value;
        var encryptor = CreateEncryptor();
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);
        user.RemoveIntegration(IntegrationProvider.GoogleAnalytics4);

        // Act
        var result = user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t2", "r2", "acc2", null, encryptor);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
