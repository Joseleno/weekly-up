namespace WeeklyUp.Infrastructure.Email.Templates;

internal static class VerificationEmailTemplate
{
    private const string BaseUrl = "https://weeklyup.app";

    private const string Styles =
        "body{margin:0;padding:0;background:#f3f4f6;font-family:'Segoe UI',Arial,sans-serif}" +
        ".container{max-width:600px;margin:32px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)}" +
        ".header{background:linear-gradient(135deg,#1d4ed8,#3b82f6);padding:48px 32px;text-align:center}" +
        ".header h1{color:#fff;margin:0 0 8px;font-size:28px}" +
        ".header p{color:#bfdbfe;margin:0;font-size:15px}" +
        ".body{padding:36px 32px;text-align:center}" +
        ".body p{color:#475569;font-size:15px;line-height:1.6;margin:0 0 24px}" +
        ".btn{display:inline-block;background:#2563eb;color:#fff;text-decoration:none;padding:14px 32px;border-radius:8px;font-size:15px;font-weight:600}" +
        ".footer{padding:24px 32px;text-align:center;color:#94a3b8;font-size:12px;border-top:1px solid #f1f5f9}";

    internal static string Build(string recipientName, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var encodedToken = Uri.EscapeDataString(token);

        return $"""
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Confirme seu email - WeeklyUp</title>
          <style>{Styles}</style>
        </head>
        <body>
        <div class="container">
          <div class="header">
            <h1>Confirme seu email</h1>
            <p>Olá, <strong>{recipientName}</strong>! Só mais um passo para ativar sua conta.</p>
          </div>
          <div class="body">
            <p>Clique no botão abaixo para confirmar seu endereço de email e ativar sua conta WeeklyUp.</p>
            <a href="{BaseUrl}/verify?token={encodedToken}" class="btn">Confirmar email →</a>
            <p style="margin-top:28px;font-size:13px;color:#94a3b8">
              Se não foi você quem se cadastrou, ignore este email.<br/>
              O link expira em 24 horas.
            </p>
          </div>
          <div class="footer">
            <p style="margin:0">WeeklyUp — Relatórios automáticos para o seu negócio</p>
          </div>
        </div>
        </body>
        </html>
        """;
    }
}
