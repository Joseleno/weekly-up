namespace WeeklyUp.Infrastructure.Email.Templates;

public static class WelcomeEmailTemplate
{
    public static string Build(string recipientName) =>
        $"""
        <!DOCTYPE html><html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto">
        <h1 style="color:#2563eb">Bem-vindo ao WeeklyUp!</h1>
        <p>Ola, <strong>{recipientName}</strong>!</p>
        <p>Voce esta a um passo de receber relatorios semanais automaticos do seu negocio.</p>
        <p>Conecte suas integracoes (Google Analytics, Stripe) para comecar a receber insights.</p>
        <p><a href="https://weeklyup.app/integrations" style="background:#2563eb;color:white;padding:12px 24px;text-decoration:none;border-radius:6px">Conectar Integracoes</a></p>
        </body></html>
        """;
}
