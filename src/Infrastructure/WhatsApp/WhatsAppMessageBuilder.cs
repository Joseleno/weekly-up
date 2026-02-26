using System.Globalization;
using System.Text;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Infrastructure.WhatsApp;

public static class WhatsAppMessageBuilder
{
    private const string DashboardUrl = "https://weeklyup.app/dashboard";

    public static string Build(string recipientName, Report report)
    {
        var metrics = report.Metrics;
        var insights = report.Insights;
        var week = report.WeekRange.ToString();

        var sb = new StringBuilder();

        AppendHeader(sb, recipientName, week);
        AppendMetrics(sb, metrics);

        if (insights is not null)
        {
            AppendInsights(sb, insights);
        }

        AppendFooter(sb);

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, string name, string week)
    {
        sb.AppendLine("📊 *Relatório Semanal WeeklyUp*");
        sb.AppendLine();
        sb.AppendLine("Olá, *" + name + "*! 👋");
        sb.AppendLine("Aqui está o resumo do seu negócio de *" + week + "*:");
        sb.AppendLine();
    }

    private static void AppendMetrics(StringBuilder sb, ReportMetrics? metrics)
    {
        sb.AppendLine("💰 *Métricas da Semana*");
        sb.AppendLine();

        var revenue = FormatBrl(metrics?.Revenue.Amount ?? 0);
        var revenueChange = BuildChangeText(metrics?.Revenue.Amount, metrics?.PreviousRevenue?.Amount);
        sb.AppendLine("📈 Receita: *R$ " + revenue + "* " + revenueChange);

        var sales = metrics?.SalesCount.ToString(CultureInfo.InvariantCulture) ?? "0";
        var avgTicket = FormatBrl(metrics?.AverageTicket.Amount ?? 0);
        sb.AppendLine("🛒 Vendas: *" + sales + "* (ticket médio R$ " + avgTicket + ")");

        var visits = metrics?.TotalVisits.ToString("N0", CultureInfo.InvariantCulture) ?? "0";
        var visitsChange = BuildChangeText(metrics?.TotalVisits, metrics?.PreviousVisits);
        sb.AppendLine("👥 Visitas: *" + visits + "* " + visitsChange);

        var newCustomers = metrics?.NewCustomers.ToString(CultureInfo.InvariantCulture) ?? "0";
        sb.AppendLine("🆕 Novos clientes: *" + newCustomers + "*");

        if (!string.IsNullOrWhiteSpace(metrics?.TopTrafficSource))
        {
            sb.AppendLine("🔗 Principal fonte: *" + metrics.TopTrafficSource + "*");
        }

        sb.AppendLine();
    }

    private static void AppendInsights(StringBuilder sb, ReportInsights insights)
    {
        sb.AppendLine("🤖 *Insights da IA*");
        sb.AppendLine();
        sb.AppendLine("✅ *Destaque:* " + insights.Highlight);
        sb.AppendLine();
        sb.AppendLine("⚠️ *Alerta:* " + insights.Alert);
        sb.AppendLine();
        sb.AppendLine("💡 *Dica:* " + insights.Tip);
        sb.AppendLine();
    }

    private static void AppendFooter(StringBuilder sb)
    {
        sb.AppendLine("─────────────────");
        sb.AppendLine("🔗 Ver dashboard completo:");
        sb.AppendLine(DashboardUrl);
        sb.AppendLine();
        sb.AppendLine("_WeeklyUp — Relatórios automáticos para o seu negócio_ 🚀");
    }

    private static string FormatBrl(decimal value) =>
        value.ToString("N2", new CultureInfo("pt-BR"));

    private static string BuildChangeText(decimal? current, decimal? previous)
    {
        if (current is null || previous is null || previous == 0)
        {
            return string.Empty;
        }

        var pct = ((current.Value - previous.Value) / previous.Value) * 100m;
        var pctStr = pct.ToString("F1", CultureInfo.InvariantCulture);
        var absPctStr = Math.Abs(pct).ToString("F1", CultureInfo.InvariantCulture);
        return pct >= 0
            ? "_(↑ " + pctStr + "% vs semana anterior)_"
            : "_(↓ " + absPctStr + "% vs semana anterior)_";
    }

    private static string BuildChangeText(int? current, int? previous) =>
        BuildChangeText((decimal?)current, (decimal?)previous);
}
