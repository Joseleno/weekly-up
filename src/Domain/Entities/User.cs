using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<Integration> _integrations = [];

    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public BusinessName BusinessName { get; private set; } = null!;
    public BusinessType BusinessType { get; private set; }
    private const string DefaultTimezone = "America/Sao_Paulo";

    public string Timezone { get; private set; } = DefaultTimezone;
    public PlanType Plan { get; private set; }
    public string? ExternalAuthId { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? VerificationToken { get; private set; }
    public IReadOnlyCollection<Integration> Integrations => _integrations.AsReadOnly();

    private User() { }

    public static Result<User> Create(
        string email,
        string name,
        string businessName,
        BusinessType businessType,
        string? externalAuthId = null,
        string? verificationToken = null)
    {
        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        Result<BusinessName> businessNameResult = BusinessName.Create(businessName);
        if (businessNameResult.IsFailure)
        {
            return businessNameResult.Error;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return AppError.Validation("User.NameEmpty", "Nome nao pode ser vazio.");
        }

        User user = new()
        {
            Email = emailResult.Value,
            Name = name.Trim(),
            BusinessName = businessNameResult.Value,
            BusinessType = businessType,
            Plan = PlanType.Free,
            IsActive = true,
            IsEmailVerified = false,
            ExternalAuthId = externalAuthId,
            VerificationToken = verificationToken,
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email.Value, user.Name, verificationToken));
        return user;
    }

    public Result<Integration> AddIntegration(
        IntegrationProvider provider,
        string accessToken,
        string refreshToken,
        string providerAccountId,
        string? propertyId,
        ITokenEncryptor encryptor)
    {
        int limit = GetIntegrationLimit();
        if (_integrations.Count(i => i.Status != IntegrationStatus.Disconnected) >= limit)
        {
            return AppError.Validation(
                "User.IntegrationLimitReached",
                $"Plano {Plan} permite no maximo {limit} integracoes ativas.");
        }

        Integration? existing = _integrations.FirstOrDefault(
            i => i.Provider == provider && i.Status != IntegrationStatus.Disconnected);

        if (existing is not null)
        {
            return AppError.Conflict(
                "User.IntegrationAlreadyConnected",
                $"Integracao com {provider} ja esta conectada.");
        }

        Integration integration = Integration.Create(
            Id, provider, accessToken, refreshToken, providerAccountId, propertyId, encryptor);

        _integrations.Add(integration);
        RaiseDomainEvent(new IntegrationConnectedEvent(Id, provider));
        return integration;
    }

    public Result<bool> RemoveIntegration(IntegrationProvider provider)
    {
        Integration? integration = _integrations.FirstOrDefault(
            i => i.Provider == provider && i.Status != IntegrationStatus.Disconnected);

        if (integration is null)
        {
            return AppError.NotFound(
                "User.IntegrationNotFound",
                $"Integracao com {provider} nao encontrada.");
        }

        integration.Disconnect();
        RaiseDomainEvent(new IntegrationDisconnectedEvent(Id, provider));
        return true;
    }

    public Result<bool> UpgradePlan(PlanType newPlan)
    {
        if (newPlan <= Plan)
        {
            return AppError.Validation(
                "User.InvalidPlanUpgrade",
                $"Novo plano deve ser superior ao plano atual ({Plan}).");
        }

        PlanType previousPlan = Plan;
        Plan = newPlan;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserPlanUpgradedEvent(Id, previousPlan, newPlan));
        return true;
    }

    public void SetPlanFromWebhook(PlanType newPlan)
    {
        if (Plan == newPlan)
        {
            return;
        }
        PlanType previousPlan = Plan;
        Plan = newPlan;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserPlanChangedEvent(Id, previousPlan, newPlan));
    }

    public void SetVerificationToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        VerificationToken = token;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result VerifyEmail(string token)
    {
        if (IsEmailVerified)
        {
            return Result.Failure(AppError.Validation("User.AlreadyVerified", "Email ja verificado."));
        }

        if (string.IsNullOrWhiteSpace(VerificationToken) || VerificationToken != token)
        {
            return Result.Failure(AppError.Validation("User.InvalidVerificationToken", "Token de verificacao invalido."));
        }

        IsEmailVerified = true;
        VerificationToken = null;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserEmailVerifiedEvent(Id));
        return Result.Success();
    }

    public Result<bool> UpdateProfile(string name, string businessName, BusinessType businessType)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AppError.Validation("User.NameEmpty", "Nome nao pode ser vazio.");
        }

        Result<BusinessName> businessNameResult = BusinessName.Create(businessName);
        if (businessNameResult.IsFailure)
        {
            return businessNameResult.Error;
        }

        Name = name.Trim();
        BusinessName = businessNameResult.Value;
        BusinessType = businessType;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void SetStripeCustomerId(string customerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        StripeCustomerId = customerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanAccessDemographics() => Plan >= PlanType.Pro;
    public bool CanAccessInsights() => Plan >= PlanType.Pro;
    public bool CanAccessWhatsApp() => Plan >= PlanType.Business;

    private int GetIntegrationLimit() => Plan switch
    {
        PlanType.Free => PlanLimits.FreeMaxIntegrations,
        PlanType.Pro => PlanLimits.ProMaxIntegrations,
        PlanType.Business => PlanLimits.BusinessMaxIntegrations,
        _ => PlanLimits.FreeMaxIntegrations,
    };
}
