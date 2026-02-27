using System.Reflection;

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
    private const string DefaultToken = "test-verification-token";

    private static ITokenEncryptor CreateEncryptor()
    {
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns(x => $"enc_{x.Arg<string>()}");
        return encryptor;
    }

    private static User CreateUser(
        string email = "user@test.com",
        string name = "Jo\u00e3o",
        string businessName = "Loja",
        BusinessType businessType = BusinessType.Ecommerce,
        string? verificationToken = DefaultToken)
    {
        return User.Create(email, name, businessName, businessType, verificationToken: verificationToken).Value;
    }

    // ── Create ──────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsSuccess()
    {
        // Act
        var result = User.Create("user@test.com", "Jo\u00e3o Silva", "Minha Loja", BusinessType.Ecommerce, verificationToken: DefaultToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Value.Should().Be("user@test.com");
        result.Value.Name.Should().Be("Jo\u00e3o Silva");
        result.Value.Plan.Should().Be(PlanType.Free);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsEmailVerified.Should().BeFalse();
        result.Value.VerificationToken.Should().Be(DefaultToken);
        result.Value.VerificationTokenExpiresAt.Should().NotBeNull();
        result.Value.VerificationTokenExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_InvalidEmail_ReturnsFailure()
    {
        // Act
        var result = User.Create("nao-e-email", "Jo\u00e3o", "Loja", BusinessType.Ecommerce, verificationToken: DefaultToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidFormat");
    }

    [Fact]
    public void Create_EmptyName_ReturnsFailure()
    {
        // Act
        var result = User.Create("user@test.com", "   ", "Loja", BusinessType.Ecommerce, verificationToken: DefaultToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NameEmpty");
    }

    [Fact]
    public void Create_InvalidBusinessName_ReturnsFailure()
    {
        // Act
        var result = User.Create("user@test.com", "Jo\u00e3o", "A", BusinessType.Ecommerce, verificationToken: DefaultToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BusinessName.TooShort");
    }

    [Fact]
    public void Create_RaisesUserRegisteredEvent()
    {
        // Act
        var result = User.Create("user@test.com", "Jo\u00e3o", "Minha Loja", BusinessType.Ecommerce, verificationToken: DefaultToken);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public void Create_WithExternalAuthId_SetsExternalAuthIdAndVerifiesEmail()
    {
        // Act
        var result = User.Create("user@test.com", "Jo\u00e3o", "Minha Loja", BusinessType.Ecommerce, "google|123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExternalAuthId.Should().Be("google|123");
        result.Value.IsEmailVerified.Should().BeTrue();
        result.Value.VerificationToken.Should().BeNull();
        result.Value.VerificationTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutExternalAuthId_AndWithoutToken_ReturnsValidationError()
    {
        // Act
        var result = User.Create("user@test.com", "Jo\u00e3o", "Minha Loja", BusinessType.Ecommerce);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.VerificationTokenRequired");
    }

    // ── AddIntegration ──────────────────────────────────────

    [Fact]
    public void AddIntegration_FreePlan_FirstIntegration_Succeeds()
    {
        // Arrange
        var user = CreateUser();
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
        var user = CreateUser();
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
        var user = CreateUser();
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
        var user = CreateUser();
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
        var user = CreateUser();
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
        var user = CreateUser();

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
        var user = CreateUser();
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
        var user = CreateUser();
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
        var user = CreateUser();

        // Act
        var result = user.UpgradePlan(PlanType.Free);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ── SetPlanFromWebhook ──────────────────────────────────

    [Fact]
    public void SetPlanFromWebhook_WhenPlanAlreadySame_DoesNotRaiseEvent()
    {
        // Arrange
        var user = CreateUser(email: "a@b.com", name: "N");
        user.SetPlanFromWebhook(PlanType.Pro);
        user.ClearDomainEvents();

        // Act
        user.SetPlanFromWebhook(PlanType.Pro); // mesmo plano

        // Assert
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetPlanFromWebhook_WhenDowngradeToFree_EmitsUserPlanChangedEvent()
    {
        // Arrange
        var user = CreateUser(email: "a@b.com", name: "N");
        user.SetPlanFromWebhook(PlanType.Pro);
        user.ClearDomainEvents();

        // Act
        user.SetPlanFromWebhook(PlanType.Free);

        // Assert
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserPlanChangedEvent>();
        var ev = (UserPlanChangedEvent)user.DomainEvents.First();
        ev.PreviousPlan.Should().Be(PlanType.Pro);
        ev.NewPlan.Should().Be(PlanType.Free);
    }

    // ── VerifyEmail ─────────────────────────────────────────

    [Fact]
    public void VerifyEmail_WithValidToken_SetsIsEmailVerifiedAndRaisesEvent()
    {
        // Arrange
        var user = CreateUser();
        var token = Guid.NewGuid().ToString("N");
        user.SetVerificationToken(token);
        user.ClearDomainEvents();

        // Act
        var result = user.VerifyEmail(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.VerificationToken.Should().BeNull();
        user.VerificationTokenExpiresAt.Should().BeNull();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserEmailVerifiedEvent>();
    }

    [Fact]
    public void VerifyEmail_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var user = CreateUser();
        user.SetVerificationToken("valid-token");

        // Act
        var result = user.VerifyEmail("wrong-token");

        // Assert
        result.IsFailure.Should().BeTrue();
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_ReturnsFailure()
    {
        // Arrange
        var user = CreateUser();
        var token = Guid.NewGuid().ToString("N");
        user.SetVerificationToken(token);
        user.VerifyEmail(token);

        // Act
        var result = user.VerifyEmail(token);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmail_WhenTokenExpired_ReturnsTokenExpiredError()
    {
        // Arrange
        var user = CreateUser();
        var token = Guid.NewGuid().ToString("N");
        user.SetVerificationTokenWithExpiry(token, DateTime.UtcNow.AddHours(-1));

        // Act
        var result = user.VerifyEmail(token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.VerificationTokenExpired");
    }

    // ── Capability checks ───────────────────────────────────

    [Fact]
    public void CanAccessDemographics_Free_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        user.CanAccessDemographics().Should().BeFalse();
    }

    [Fact]
    public void CanAccessDemographics_Pro_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser();
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessDemographics().Should().BeTrue();
    }

    [Fact]
    public void CanAccessWhatsApp_ProPlan_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser();
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessWhatsApp().Should().BeFalse();
    }

    [Fact]
    public void CanAccessWhatsApp_BusinessPlan_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser();
        user.UpgradePlan(PlanType.Pro);
        user.UpgradePlan(PlanType.Business);

        // Act & Assert
        user.CanAccessWhatsApp().Should().BeTrue();
    }

    [Fact]
    public void CanAccessInsights_FreePlan_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        user.CanAccessInsights().Should().BeFalse();
    }

    [Fact]
    public void CanAccessInsights_ProPlan_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser();
        user.UpgradePlan(PlanType.Pro);

        // Act & Assert
        user.CanAccessInsights().Should().BeTrue();
    }

    // ── SetPhoneNumber ───────────────────────────────────────

    [Fact]
    public void SetPhoneNumber_ValidNumber_SetsPhoneNumber()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.SetPhoneNumber("+5511999999999");

        // Assert
        user.PhoneNumber.Should().Be("+5511999999999");
    }

    [Fact]
    public void SetPhoneNumber_Null_ClearsPhoneNumber()
    {
        // Arrange
        var user = CreateUser();
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
        var user = CreateUser(businessName: "Loja Antiga");

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
        var user = CreateUser();

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
        var user = CreateUser();

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
        var user = CreateUser();
        var encryptor = CreateEncryptor();
        user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t1", "r1", "acc1", null, encryptor);
        user.RemoveIntegration(IntegrationProvider.GoogleAnalytics4);

        // Act
        var result = user.AddIntegration(IntegrationProvider.GoogleAnalytics4, "t2", "r2", "acc2", null, encryptor);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
