namespace WeeklyUp.Infrastructure.Instagram;

public sealed class InstagramOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    // URI do WebApp (não da API) — Meta redireciona o browser para o WebApp
    public string RedirectUri { get; init; } = string.Empty;
}
