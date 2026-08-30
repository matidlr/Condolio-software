using Condolio.Domain.Common;

namespace Condolio.Domain.Contactos;

/// <summary>Contacto útil de la comunidad (proveedor, servicio, administración, emergencias…).</summary>
public class Contacto : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    /// <summary>Rubro libre: "Plomero", "Electricista", "Administración", "Emergencia", "Otro"…</summary>
    public string Categoria { get; set; } = "Otro";

    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Empresa { get; set; }
    public string? Notas { get; set; }

    public string CreadoPorUsuarioId { get; set; } = string.Empty;
    public string CreadoPorNombre { get; set; } = string.Empty;
}
