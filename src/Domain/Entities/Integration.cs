using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Services;

namespace WeeklyUp.Domain.Entities;

public sealed class Integration : Entity
{
    private const int MaxLastErrorLength = 500;
    private const string TokenExpiredMessage = "Token expirado.";

    public Guid UserId { get; private set; }
    public IntegrationProvider Provider { get; private set; }
    public string AccessToken { get; private set; } = string.Empty;
    public string RefreshToken { get; private set; } = string.Empty;
    public string ProviderAccountId { get; private set; } = string.Empty;
    public string? PropertyId { get; private set; }
    public IntegrationStatus Status { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? LastError { get; private set; }

    private Integration() { }

    internal static Integration Create(
        Guid userId,
        IntegrationProvider provider,
        string accessToken,
        string refreshToken,
        string providerAccountId,
        string? propertyId,
        ITokenEncryptor encryptor)
    {
        ArgumentNullException.ThrowIfNull(encryptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAccountId);

        return new Integration
        {
            UserId = userId,
            Provider = provider,
            AccessToken = encryptor.Encrypt(accessToken),
            RefreshToken = encryptor.Encrypt(refreshToken),
            ProviderAccountId = providerAccountId,
            PropertyId = propertyId,
            Status = IntegrationStatus.Connected,
        };
    }

    public void UpdateTokens(string accessToken, string refreshToken, ITokenEncryptor encryptor)
    {
        ArgumentNullException.ThrowIfNull(encryptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        AccessToken = encryptor.Encrypt(accessToken);
        RefreshToken = encryptor.Encrypt(refreshToken);
        Status = IntegrationStatus.Connected;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSynced()
    {
        LastSyncAt = DateTime.UtcNow;
        Status = IntegrationStatus.Connected;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Status = IntegrationStatus.Error;
        LastError = error.Length > MaxLastErrorLength ? error[..MaxLastErrorLength] : error;
        UpdatedAt = DateTime.UtcNow;

        // O caller (User) dispara o IntegrationSyncFailedEvent
    }

    public void MarkTokenExpired()
    {
        Status = IntegrationStatus.Error;
        LastError = TokenExpiredMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        Status = IntegrationStatus.Disconnected;
        UpdatedAt = DateTime.UtcNow;
    }
}
