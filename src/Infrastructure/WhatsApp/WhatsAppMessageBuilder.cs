using System.Globalization;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.WhatsApp;

public static class WhatsAppMessageBuilder
{
    public static string Build(string recipientName, Report report)
    {
        var metrics = report.Metrics;
        string revenue = metrics?.Revenue.Amount.ToString("N2", CultureInfo.InvariantCulture) ?? "0,00";
        string sales = metrics?.SalesCount.ToString(CultureInfo.InvariantCulture) ?? "0";
        string visits = metrics?.TotalVisits.ToString(CultureInfo.InvariantCulture) ?? "0";
        var week = report.WeekRange.ToString();

        return $"*Relatorio Semanal WeeklyUp*\n\n" +
               $"Ola, {recipientName}! Resumo da semana {week}:\n\n" +
               $"Receita: R$ {revenue}\n" +
               $"Vendas: {sales}\n" +
               $"Visitas: {visits}\n\n" +
               $"Acesse seu dashboard: https://weeklyup.app/dashboard";
    }
}
