using System.Globalization;
using System.Text;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Infrastructure.Email.Templates;

public static class WeeklyReportEmailTemplate
{
    private const string DashboardUrl = "https://weeklyup.app/dashboard";
    private const string PreferencesUrl = "https://weeklyup.app/settings/preferences";
    private const string UnsubscribeUrl = "https://weeklyup.app/unsubscribe";

    public static string Build(string recipientName, Report report)
    {
        var metrics = report.Metrics;
        var insights = report.Insights;
        var demographics = report.Demographics;
        var week = report.WeekRange.ToString();

        var revenueFormatted = FormatBrl(metrics?.Revenue.Amount ?? 0);
        var avgTicketFormatted = FormatBrl(metrics?.AverageTicket.Amount ?? 0);
        var revenueChange = CalcChange(metrics?.Revenue.Amount, metrics?.PreviousRevenue?.Amount);
        var visitsChange = CalcChange(metrics?.TotalVisits, metrics?.PreviousVisits);

        var sb = new StringBuilder();
        sb.Append(BuildHead());
        sb.Append(BuildHeader(recipientName, week));
        sb.Append(BuildMetricsBlock(metrics, revenueFormatted, avgTicketFormatted, revenueChange, visitsChange));

        if (insights is not null)
        {
            sb.Append(BuildInsightsBlock(insights));
        }

        if (demographics is not null)
        {
            sb.Append(BuildDemographicsBlock(demographics));
        }

        sb.Append(BuildCta());
        sb.Append(BuildFooter());

        return sb.ToString();
    }

    private static string BuildHead() =>
        """
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Relatório Semanal WeeklyUp</title>
          <style>
            body { margin:0; padding:0; background:#f3f4f6; font-family:'Segoe UI',Arial,sans-serif; }
            .container { max-width:600px; margin:32px auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,.08); }
            .header { background:linear-gradient(135deg,#1d4ed8,#3b82f6); padding:40px 32px; text-align:center; }
            .header h1 { color:#fff; margin:0 0 4px; font-size:26px; letter-spacing:-.3px; }
            .header p { color:#bfdbfe; margin:0; font-size:14px; }
            .section { padding:28px 32px; border-bottom:1px solid #f1f5f9; }
            .section-title { font-size:13px; font-weight:700; text-transform:uppercase; letter-spacing:.8px; color:#64748b; margin:0 0 18px; }
            .metric-grid { display:grid; grid-template-columns:1fr 1fr; gap:12px; }
            .metric-card { background:#f8fafc; border-radius:8px; padding:16px; }
            .metric-label { font-size:12px; color:#64748b; margin:0 0 4px; }
            .metric-value { font-size:22px; font-weight:700; color:#0f172a; margin:0; }
            .metric-change { font-size:12px; margin:4px 0 0; }
            .up { color:#16a34a; } .down { color:#dc2626; } .neutral { color:#64748b; }
            .insight-card { border-radius:8px; padding:14px 16px; margin-bottom:10px; }
            .insight-highlight { background:#f0fdf4; border-left:4px solid #16a34a; }
            .insight-alert { background:#fff7ed; border-left:4px solid #f97316; }
            .insight-tip { background:#eff6ff; border-left:4px solid #3b82f6; }
            .insight-label { font-size:11px; font-weight:700; text-transform:uppercase; letter-spacing:.6px; margin:0 0 6px; }
            .insight-highlight .insight-label { color:#15803d; }
            .insight-alert .insight-label { color:#c2410c; }
            .insight-tip .insight-label { color:#1d4ed8; }
            .insight-text { font-size:14px; color:#1e293b; margin:0; line-height:1.5; }
            .demo-row { display:flex; justify-content:space-between; align-items:center; margin-bottom:8px; font-size:14px; }
            .demo-label { color:#475569; }
            .demo-bar-wrap { flex:1; margin:0 12px; height:6px; background:#e2e8f0; border-radius:3px; }
            .demo-bar { height:6px; border-radius:3px; background:#3b82f6; }
            .demo-value { color:#0f172a; font-weight:600; min-width:36px; text-align:right; }
            .cta-section { padding:32px; text-align:center; background:#f8fafc; }
            .btn { display:inline-block; background:#2563eb; color:#fff; text-decoration:none; padding:14px 32px; border-radius:8px; font-size:15px; font-weight:600; }
            .footer { padding:24px 32px; text-align:center; color:#94a3b8; font-size:12px; }
            .footer a { color:#64748b; }
            @media (max-width:480px) { .metric-grid { grid-template-columns:1fr; } }
          </style>
        </head>
        <body>
        <div class="container">
        """;

    private static string BuildHeader(string name, string week) =>
        $"""
          <div class="header">
            <h1>📊 Relatório Semanal</h1>
            <p>Olá, <strong>{EscapeHtml(name)}</strong>! Aqui está seu resumo de <strong>{EscapeHtml(week)}</strong></p>
          </div>
        """;

    private static string BuildMetricsBlock(
        ReportMetrics? metrics,
        string revenueFormatted,
        string avgTicketFormatted,
        (string Arrow, string Css) revenueChange,
        (string Arrow, string Css) visitsChange)
    {
        var sales = metrics?.SalesCount.ToString(CultureInfo.InvariantCulture) ?? "0";
        var visits = metrics?.TotalVisits.ToString("N0", CultureInfo.InvariantCulture) ?? "0";
        var newCustomers = metrics?.NewCustomers.ToString(CultureInfo.InvariantCulture) ?? "0";
        var topSource = EscapeHtml(metrics?.TopTrafficSource ?? "—");
        var topPage = EscapeHtml(metrics?.TopPage ?? "—");

        return $"""
          <div class="section">
            <p class="section-title">💰 Métricas da Semana</p>
            <div class="metric-grid">
              <div class="metric-card">
                <p class="metric-label">Receita Total</p>
                <p class="metric-value">R$ {revenueFormatted}</p>
                <p class="metric-change {revenueChange.Css}">{revenueChange.Arrow} vs semana anterior</p>
              </div>
              <div class="metric-card">
                <p class="metric-label">Vendas</p>
                <p class="metric-value">{sales}</p>
                <p class="metric-change neutral">Ticket médio R$ {avgTicketFormatted}</p>
              </div>
              <div class="metric-card">
                <p class="metric-label">Visitas ao Site</p>
                <p class="metric-value">{visits}</p>
                <p class="metric-change {visitsChange.Css}">{visitsChange.Arrow} vs semana anterior</p>
              </div>
              <div class="metric-card">
                <p class="metric-label">Novos Clientes</p>
                <p class="metric-value">{newCustomers}</p>
                <p class="metric-change neutral">Fonte: {topSource}</p>
              </div>
            </div>
            <p style="margin:16px 0 0;font-size:13px;color:#64748b">📄 Página mais visitada: <strong>{topPage}</strong></p>
          </div>
        """;
    }

    private static string BuildInsightsBlock(ReportInsights insights) =>
        $"""
          <div class="section">
            <p class="section-title">🤖 Insights da IA</p>
            <div class="insight-card insight-highlight">
              <p class="insight-label">✅ Destaque</p>
              <p class="insight-text">{EscapeHtml(insights.Highlight)}</p>
            </div>
            <div class="insight-card insight-alert">
              <p class="insight-label">⚠️ Alerta</p>
              <p class="insight-text">{EscapeHtml(insights.Alert)}</p>
            </div>
            <div class="insight-card insight-tip">
              <p class="insight-label">💡 Dica</p>
              <p class="insight-text">{EscapeHtml(insights.Tip)}</p>
            </div>
          </div>
        """;

    private static string BuildDemographicsBlock(Demographics demographics)
    {
        var sb = new StringBuilder();
        sb.Append("""
          <div class="section">
            <p class="section-title">👥 Dados Demográficos</p>
        """);

        if (demographics.GenderDistribution.Count > 0)
        {
            sb.Append("<p style=\"font-size:13px;color:#64748b;margin:0 0 10px\"><strong>Gênero</strong></p>");
            foreach (var (gender, pct) in demographics.GenderDistribution)
            {
                var barWidth = Math.Min((int)pct, 100).ToString(CultureInfo.InvariantCulture);
                var pctStr = pct.ToString("F0", CultureInfo.InvariantCulture);
                sb.Append("<div class=\"demo-row\">")
                  .Append("<span class=\"demo-label\">" + EscapeHtml(gender) + "</span>")
                  .Append("<div class=\"demo-bar-wrap\"><div class=\"demo-bar\" style=\"width:" + barWidth + "%\"></div></div>")
                  .Append("<span class=\"demo-value\">" + pctStr + "%</span>")
                  .Append("</div>");
            }
        }

        if (demographics.TopCities.Count > 0)
        {
            sb.Append("<p style=\"font-size:13px;color:#64748b;margin:16px 0 10px\"><strong>Top Cidades</strong></p>");
            foreach (var city in demographics.TopCities.Take(5))
            {
                sb.Append("<div class=\"demo-row\"><span class=\"demo-label\">📍 " + EscapeHtml(city) + "</span></div>");
            }
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string BuildCta() =>
        $"""
          <div class="cta-section">
            <p style="margin:0 0 20px;color:#475569;font-size:15px">Veja todos os detalhes no seu dashboard</p>
            <a href="{DashboardUrl}" class="btn">Ver Dashboard Completo →</a>
          </div>
        """;

    private static string BuildFooter() =>
        $"""
          <div class="footer">
            <p style="margin:0 0 8px">WeeklyUp — Relatórios automáticos para o seu negócio</p>
            <p style="margin:0">
              <a href="{PreferencesUrl}">Preferências de envio</a> ·
              <a href="{UnsubscribeUrl}">Cancelar inscrição</a>
            </p>
          </div>
        </div>
        </body>
        </html>
        """;

    private static string FormatBrl(decimal value) =>
        value.ToString("N2", new CultureInfo("pt-BR"));

    private static (string Arrow, string Css) CalcChange(decimal? current, decimal? previous)
    {
        if (current is null || previous is null || previous == 0)
        {
            return ("Sem dados anteriores", "neutral");
        }

        var pct = ((current.Value - previous.Value) / previous.Value) * 100m;
        var pctStr = pct.ToString("F1", CultureInfo.InvariantCulture);
        var absPctStr = Math.Abs(pct).ToString("F1", CultureInfo.InvariantCulture);
        return pct >= 0
            ? ($"↑ {pctStr}%", "up")
            : ($"↓ {absPctStr}%", "down");
    }

    private static (string Arrow, string Css) CalcChange(int? current, int? previous)
    {
        if (current is null || previous is null)
        {
            return ("Sem dados anteriores", "neutral");
        }

        return CalcChange((decimal?)current.Value, (decimal?)previous.Value);
    }

    private static string EscapeHtml(string? value) =>
        value is null ? string.Empty
        : value.Replace("&", "&amp;")
               .Replace("<", "&lt;")
               .Replace(">", "&gt;")
               .Replace("\"", "&quot;");
}
