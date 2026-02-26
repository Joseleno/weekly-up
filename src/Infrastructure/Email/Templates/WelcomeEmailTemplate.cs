namespace WeeklyUp.Infrastructure.Email.Templates;

public static class WelcomeEmailTemplate
{
    private const string IntegrationsUrl = "https://weeklyup.app/integrations";
    private const string DashboardUrl = "https://weeklyup.app/dashboard";

    private const string Styles =
        "body{margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Arial,sans-serif}" +
        ".container{max-width:600px;margin:32px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)}" +
        ".header{background:linear-gradient(135deg,#1d4ed8,#3b82f6);padding:48px 32px;text-align:center}" +
        ".header h1{color:#fff;margin:0 0 8px;font-size:28px}" +
        ".header p{color:#bfdbfe;margin:0;font-size:15px}" +
        ".body{padding:36px 32px}" +
        ".step{display:flex;gap:16px;margin-bottom:24px;align-items:flex-start}" +
        ".num{width:32px;height:32px;border-radius:50%;background:#eff6ff;color:#2563eb;font-weight:700;font-size:14px;display:flex;align-items:center;justify-content:center;flex-shrink:0}" +
        ".st h3{margin:0 0 4px;font-size:15px;color:#0f172a}" +
        ".st p{margin:0;font-size:14px;color:#64748b;line-height:1.5}" +
        ".cta{text-align:center;padding:8px 32px 36px}" +
        ".btn{display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:14px 32px;border-radius:8px;font-size:15px;font-weight:600}" +
        ".footer{padding:24px 32px;text-align:center;color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9}";

    public static string Build(string recipientName) =>
        $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Bem-vindo ao WeeklyUp!</title>
          <style>{Styles}</style>
        </head>
        <body>
        <div class="container">
          <div class="header">
            <h1>🎉 Bem-vindo ao WeeklyUp!</h1>
            <p>Olá, <strong>{recipientName}</strong>! Você está a um passo de receber relatórios semanais automáticos.</p>
          </div>
          <div class="body">
            <p style="color:#475569;font-size:15px;margin:0 0 28px">Veja como é simples começar:</p>
            <div class="step">
              <div class="num">1</div>
              <div class="st">
                <h3>Conecte suas fontes de dados</h3>
                <p>Integre o Google Analytics para métricas de tráfego e o Stripe para dados de vendas. Leva menos de 2 minutos.</p>
              </div>
            </div>
            <div class="step">
              <div class="num">2</div>
              <div class="st">
                <h3>Configure suas preferências</h3>
                <p>Escolha o dia e horário de recebimento do relatório. O padrão é toda segunda-feira às 7h (horário de Brasília).</p>
              </div>
            </div>
            <div class="step">
              <div class="num">3</div>
              <div class="st">
                <h3>Receba insights automáticos</h3>
                <p>Todo relatório vem com análise de IA — destaques, alertas e dicas personalizadas para o seu negócio.</p>
              </div>
            </div>
          </div>
          <div class="cta">
            <a href="{IntegrationsUrl}" class="btn">Conectar minhas integrações →</a>
          </div>
          <div class="footer">
            <p style="margin:0">WeeklyUp — Relatórios automáticos para o seu negócio</p>
            <p style="margin:8px 0 0"><a href="{DashboardUrl}" style="color:#64748b">Acessar Dashboard</a></p>
          </div>
        </div>
        </body>
        </html>
        """;
}
