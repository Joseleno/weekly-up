namespace WeeklyUp.WebApp.Models;

internal static class BusinessTypeOptions
{
    // DEVE corresponder exatamente ao enum BusinessType em Domain/Enums/BusinessType.cs:
    // Ecommerce = 1, Services = 2, Content = 3, Other = 99
    internal static readonly (string Value, string Label)[] All =
    [
        ("Ecommerce", "E-commerce"),
        ("Services", "Serviços"),
        ("Content", "Conteúdo / Criador"),
        ("Other", "Outro"),
    ];
}
