using Microsoft.AspNetCore.Identity;

namespace Condolio.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Tenant al que pertenece el usuario (null para SuperAdmin).</summary>
    public Guid? AdministradorId { get; set; }

    /// <summary>Código de 4 dígitos para verificar el correo al registrarse.</summary>
    public string? CodigoVerificacion { get; set; }
    public DateTime? CodigoVerificacionExpiraUtc { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Administrador = "Administrador";
    public const string Residente = "Residente";

    public static readonly string[] Todos = [SuperAdmin, Administrador, Residente];
}
