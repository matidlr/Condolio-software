namespace Condolio.Application.Common;

/// <summary>Datos para emitir un JWT de acceso.</summary>
public record TokenRequest(string UsuarioId, string Email, IEnumerable<string> Roles, Guid? AdministradorId);

public interface IJwtTokenGenerator
{
    /// <summary>Devuelve (token, expiraciónUtc).</summary>
    (string Token, DateTime ExpiraUtc) Generar(TokenRequest request);
}

public interface IEmailSender
{
    Task EnviarAsync(string para, string asunto, string cuerpoHtml, CancellationToken ct = default);
}

/// <summary>Claims propios de Condolio dentro del JWT.</summary>
public static class CondolioClaims
{
    public const string TenantId = "tenantId";
}
