using System.Security.Claims;
using System.Text.Json;

using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;

namespace WeeklyUp.WebApp.Services;

public sealed class JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return [];
        }

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        if (keyValuePairs is null)
        {
            return [];
        }

        return keyValuePairs.SelectMany(kvp => ExtractClaims(kvp.Key, kvp.Value));
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64.Replace('-', '+').Replace('_', '/'));
    }

    private static IEnumerable<Claim> ExtractClaims(string key, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in value.EnumerateArray())
            {
                yield return new Claim(key, element.GetRawText().Trim('"'));
            }
        }
        else
        {
            yield return new Claim(key, value.GetRawText().Trim('"'));
        }
    }
}
