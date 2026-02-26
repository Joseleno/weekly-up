using System.Globalization;
using System.Text;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Infrastructure.AI;

internal static class InsightPromptBuilder
{
    public static string Build(ReportMetrics metrics, BusinessType businessType, string language)
    {
        var sb = new StringBuilder();

        AppendContext(sb, businessType, language);
        AppendMetrics(sb, metrics);
        AppendInstructions(sb, language);

        return sb.ToString();
    }

    private static void AppendContext(StringBuilder sb, BusinessType businessType, string language)
    {
        var bizLabel = businessType switch
        {
            BusinessType.Ecommerce => "e-commerce (loja online)",
            BusinessType.Services => "prestação de serviços",
            BusinessType.Content => "criação de conteúdo",
            _ => "negócio",
        };

        sb.AppendLine("Você é um analista de negócios especialista em " + bizLabel + ".");
        sb.AppendLine("Analise as métricas semanais abaixo e gere insights práticos e objetivos em " + language + ".");
        sb.AppendLine();
    }

    private static void AppendMetrics(StringBuilder sb, ReportMetrics metrics)
    {
        var revenue = metrics.Revenue.Amount.ToString("N2", new CultureInfo("pt-BR"));
        var salesCount = metrics.SalesCount.ToString(CultureInfo.InvariantCulture);
        var avgTicket = metrics.AverageTicket.Amount.ToString("N2", new CultureInfo("pt-BR"));
        var newCustomers = metrics.NewCustomers.ToString(CultureInfo.InvariantCulture);
        var totalVisits = metrics.TotalVisits.ToString("N0", CultureInfo.InvariantCulture);
        var uniqueVisitors = metrics.UniqueVisitors.ToString("N0", CultureInfo.InvariantCulture);
        var pageViews = metrics.PageViews.ToString("N0", CultureInfo.InvariantCulture);

        sb.AppendLine("## Métricas desta semana:");
        sb.AppendLine("- Receita total: R$ " + revenue);
        sb.AppendLine("- Número de vendas: " + salesCount);
        sb.AppendLine("- Ticket médio: R$ " + avgTicket);
        sb.AppendLine("- Novos clientes: " + newCustomers);
        sb.AppendLine("- Total de visitas: " + totalVisits);
        sb.AppendLine("- Visitantes únicos: " + uniqueVisitors);
        sb.AppendLine("- Páginas visualizadas: " + pageViews);

        if (!string.IsNullOrWhiteSpace(metrics.TopPage))
        {
            sb.AppendLine("- Página mais visitada: " + metrics.TopPage);
        }

        if (!string.IsNullOrWhiteSpace(metrics.TopTrafficSource))
        {
            sb.AppendLine("- Principal fonte de tráfego: " + metrics.TopTrafficSource);
        }

        AppendComparison(sb, metrics);
        sb.AppendLine();
    }

    private static void AppendComparison(StringBuilder sb, ReportMetrics metrics)
    {
        if (metrics.PreviousRevenue is not null)
        {
            var revChange = CalcChange(metrics.Revenue.Amount, metrics.PreviousRevenue.Amount);
            sb.AppendLine("- Variação de receita vs semana anterior: " + revChange);
        }

        if (metrics.PreviousVisits is not null)
        {
            var visChange = CalcChange(metrics.TotalVisits, metrics.PreviousVisits.Value);
            sb.AppendLine("- Variação de visitas vs semana anterior: " + visChange);
        }
    }

    private static void AppendInstructions(StringBuilder sb, string language)
    {
        sb.AppendLine("## Instruções:");
        sb.AppendLine("Gere exatamente 3 insights em formato JSON, sem texto adicional:");
        sb.AppendLine("1. **highlight**: Um ponto positivo concreto e específico (máx. 120 caracteres).");
        sb.AppendLine("2. **alert**: Um ponto de atenção ou risco identificado (máx. 120 caracteres).");
        sb.AppendLine("3. **tip**: Uma ação prática que o dono do negócio pode tomar AGORA (máx. 150 caracteres).");
        sb.AppendLine();
        sb.AppendLine("Regras:");
        sb.AppendLine("- Seja específico com os números das métricas.");
        sb.AppendLine("- Evite frases genéricas como 'continue assim' ou 'monitore de perto'.");
        sb.AppendLine("- Escreva em português do Brasil de forma direta e acessível.");
        sb.AppendLine("- Responda APENAS com JSON válido, sem markdown, sem explicações.");
        sb.AppendLine();
        sb.AppendLine("Responda em " + language + ".");
        sb.AppendLine();
        sb.AppendLine("Formato exato da resposta:");
        sb.AppendLine("{\"highlight\":\"...\",\"alert\":\"...\",\"tip\":\"...\"}");
    }

    private static string CalcChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return "N/A (sem dados anteriores)";
        }

        var pct = ((current - previous) / previous) * 100m;
        var pctStr = pct.ToString("F1", CultureInfo.InvariantCulture);
        return pct >= 0
            ? "+" + pctStr + "% (crescimento)"
            : pctStr + "% (queda)";
    }
}
