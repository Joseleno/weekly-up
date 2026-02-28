namespace WeeklyUp.WebApp.Services;

public interface IAuthService
{
    public Task<string?> GetTokenAsync();

    public Task SetTokenAsync(string token, DateTimeOffset expiresAt);

    public Task LogoutAsync();
}
