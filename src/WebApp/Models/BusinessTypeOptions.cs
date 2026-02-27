namespace WeeklyUp.WebApp.Models;

internal static class BusinessTypeOptions
{
    internal static readonly (string Value, string Label)[] All =
    [
        ("Ecommerce", "E-commerce"),
        ("Services", "Serviços"),
        ("Food", "Alimentação"),
        ("Retail", "Varejo físico"),
        ("Education", "Educação"),
        ("Health", "Saúde e Bem-estar"),
        ("Other", "Outro"),
    ];
}
