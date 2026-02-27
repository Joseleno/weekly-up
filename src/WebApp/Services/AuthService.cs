using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;

namespace WeeklyUp.WebApp.Services;

public sealed class AuthService(
    ILocalStorageService localStorage,
    AuthenticationStateProvider authStateProvider)
{
    private const string TokenKey = "auth_token";
    private const string TokenExpiryKey = "auth_token_expiry";

    public async Task<string?> GetTokenAsync()
    {
        var expiry = await localStorage.GetItemAsStringAsync(TokenExpiryKey);

        if (expiry is not null && DateTimeOffset.TryParse(expiry, out var expiresAt)
            && expiresAt <= DateTimeOffset.UtcNow)
        {
            await LogoutAsync();
            return null;
        }

        return await localStorage.GetItemAsStringAsync(TokenKey);
    }

    public async Task SetTokenAsync(string token, DateTimeOffset expiresAt)
    {
        await localStorage.SetItemAsStringAsync(TokenKey, token);
        await localStorage.SetItemAsStringAsync(TokenExpiryKey, expiresAt.ToString("O"));

        if (authStateProvider is JwtAuthenticationStateProvider jwtProvider)
        {
            jwtProvider.NotifyAuthStateChanged();
        }
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync(TokenKey);
        await localStorage.RemoveItemAsync(TokenExpiryKey);

        if (authStateProvider is JwtAuthenticationStateProvider jwtProvider)
        {
            jwtProvider.NotifyAuthStateChanged();
        }
    }
}
