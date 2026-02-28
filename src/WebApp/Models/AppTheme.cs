using MudBlazor;

namespace WeeklyUp.WebApp.Models;

internal static class AppTheme
{
    internal static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1E88E5",
            Secondary = "#FF6F00",
            AppbarBackground = "#1E88E5",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#42A5F5",
            Secondary = "#FFB74D",
        },
    };
}
