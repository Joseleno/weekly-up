namespace WeeklyUp.Shared.Extensions;

public static class StringExtensions
{
    public static string TrimSafe(this string? value) =>
        value?.Trim() ?? string.Empty;

    public static bool IsNullOrWhiteSpace(this string? value) =>
        string.IsNullOrWhiteSpace(value);

    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
