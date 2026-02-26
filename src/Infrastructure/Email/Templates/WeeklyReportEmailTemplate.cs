using System.Globalization;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Infrastructure.Email.Templates;

public static class WeeklyReportEmailTemplate
{
    public static string Build(string recipientName, Report report)
    {
        var metrics = report.Metrics;
        string revenue = metrics?.Revenue.Amount.ToString("N2", CultureInfo.InvariantCulture) ?? "0,00";
        string sales = metrics?.SalesCount.ToString(CultureInfo.InvariantCulture) ?? "0";
        string visits = metrics?.TotalVisits.ToString(CultureInfo.InvariantCulture) ?? "0";
        var week = report.WeekRange.ToString();

        return $"""
            <!DOCTYPE html><html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
            <h1 style="color:#2563eb">Relatorio Semanal</h1>
            <p>Ola, <strong>{recipientName}</strong>! Aqui esta seu resumo da semana {week}:</p>
            <table style="width:100%;border-collapse:collapse">
                <tr><td style="padding:8px;border:1px solid #e5e7eb">Receita</td><td style="padding:8px;border:1px solid #e5e7eb">R$ {revenue}</td></tr>
                <tr><td style="padding:8px;border:1px solid #e5e7eb">Vendas</td><td style="padding:8px;border:1px solid #e5e7eb">{sales}</td></tr>
                <tr><td style="padding:8px;border:1px solid #e5e7eb">Visitas</td><td style="padding:8px;border:1px solid #e5e7eb">{visits}</td></tr>
            </table>
            <p style="margin-top:24px"><a href="https://weeklyup.app/dashboard" style="background:#2563eb;color:white;padding:12px 24px;text-decoration:none;border-radius:6px">Ver Dashboard Completo</a></p>
            <p style="color:#6b7280;font-size:12px">WeeklyUp - Relatorios automaticos para o seu negocio.</p>
            </body></html>
            """;
    }
}
