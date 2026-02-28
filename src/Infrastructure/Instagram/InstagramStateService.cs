using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Instagram;

public sealed class InstagramStateService
{
    private static class StateConstants
    {
        public const int ExpirationMinutes = 10;
        public const char Separator = ':';
    }

    private readonly byte[] _signingKey;

    public InstagramStateService(IOptions<InstagramOptions> opts)
    {
        _signingKey = SHA256.HashData(Encoding.UTF8.GetBytes(opts.Value.ClientSecret));
    }

    public string GenerateState(Guid userId)
    {
        string payload = string.Concat(
            userId.ToString(),
            StateConstants.Separator,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        string signature = ComputeHmac(payload);
        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                string.Concat(payload, StateConstants.Separator, signature)));
    }

    public Result<Guid> ValidateState(string state)
    {
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(state));
        }
        catch (FormatException)
        {
            return AppError.Validation("Instagram.InvalidState", "Estado OAuth invalido.");
        }

        string[] parts = decoded.Split(StateConstants.Separator);
        if (parts.Length != 3)
        {
            return AppError.Validation("Instagram.InvalidState", "Estado OAuth invalido.");
        }

        string payload = string.Concat(parts[0], StateConstants.Separator, parts[1]);
        string expectedSig = ComputeHmac(payload);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[2]),
            Encoding.UTF8.GetBytes(expectedSig)))
        {
            return AppError.Validation("Instagram.InvalidState", "Assinatura do estado invalida.");
        }

        if (!long.TryParse(parts[1], System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out long timestamp))
        {
            return AppError.Validation("Instagram.InvalidState", "Estado OAuth invalido.");
        }

        var issued = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (DateTimeOffset.UtcNow - issued > TimeSpan.FromMinutes(StateConstants.ExpirationMinutes))
        {
            return AppError.Validation("Instagram.StateExpired", "OAuth expirado. Tente novamente.");
        }

        if (!Guid.TryParse(parts[0], out Guid userId))
        {
            return AppError.Validation("Instagram.InvalidState", "Estado OAuth invalido.");
        }

        return userId;
    }

    private string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }
}
