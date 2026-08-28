using Condolio.Application.Common;
using Microsoft.Extensions.Logging;

namespace Condolio.Infrastructure.Email;

/// <summary>Sender de desarrollo: escribe el mail al log. Reemplazar por SMTP en prod.</summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task EnviarAsync(string para, string asunto, string cuerpoHtml, CancellationToken ct = default)
    {
        _logger.LogInformation("EMAIL -> {Para} | {Asunto}\n{Cuerpo}", para, asunto, cuerpoHtml);
        return Task.CompletedTask;
    }
}
