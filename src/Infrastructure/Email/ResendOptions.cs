using System.ComponentModel.DataAnnotations;

namespace WeeklyUp.Infrastructure.Email;

public sealed class ResendOptions
{
    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string FromEmail { get; init; } = string.Empty;

    [Required]
    public string FromName { get; init; } = string.Empty;
}
