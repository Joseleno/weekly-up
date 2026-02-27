using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;

namespace WeeklyUp.WebApp.Services;

public sealed class AuthService(
    ILocalStorageService localStorage,
    AuthenticationStateProvider authStateProvider)
{
    public async Task<string?> GetTokenAsync()
    {
        var expiry = await localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey);

        if (expiry is not null && DateTimeOffset.TryParse(expiry, out var expiresAt)
            && expiresAt <= DateTimeOffset.UtcNow)
        {
            await LogoutAsync();
            return null;
        }

        return await localStorage.GetItemAsStringAsync(AuthConstants.TokenKey);
    }

    public async Task SetTokenAsync(string token, DateTimeOffset expiresAt)
    {
        await localStorage.SetItemAsStringAsync(AuthConstants.TokenKey, token);
        await localStorage.SetItemAsStringAsync(AuthConstants.TokenExpiryKey, expiresAt.ToString("O"));
        NotifyAuthStateChanged();
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync(AuthConstants.TokenKey);
        await localStorage.RemoveItemAsync(AuthConstants.TokenExpiryKey);
        NotifyAuthStateChanged();
    }

    private void NotifyAuthStateChanged()
    {
        if (authStateProvider is JwtAuthenticationStateProvider jwtProvider)
        {
            jwtProvider.NotifyAuthStateChanged();
        }
    }
}
