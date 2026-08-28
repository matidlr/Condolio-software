using System.Net;
using System.Net.Mail;
using Condolio.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Condolio.Infrastructure.Email;

public class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@condolio.app";
    public string FromNombre { get; set; } = "Condolio";
    public bool EnableSsl { get; set; } = true;
}

/// <summary>Envía correos por SMTP cuando <c>Smtp:Host</c> está configurado.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _cfg;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _cfg = config.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
        _logger = logger;
    }

    public static bool Configurado(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Smtp:Host"]);

    public async Task EnviarAsync(string para, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        using var msg = new MailMessage
        {
            From = new MailAddress(_cfg.From, _cfg.FromNombre),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true,
        };
        msg.To.Add(para);

        using var client = new SmtpClient(_cfg.Host!, _cfg.Port) { EnableSsl = _cfg.EnableSsl };
        if (!string.IsNullOrWhiteSpace(_cfg.User))
            client.Credentials = new NetworkCredential(_cfg.User, _cfg.Password);

        try
        {
            await client.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo a {Para}", para);
            throw;
        }
    }
}
