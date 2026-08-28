using Condolio.Domain.Common;

namespace Condolio.Domain.Unidades;

public enum TipoActividad
{
    UnidadCreada = 0,
    DetallesActualizados = 1,
    OcupacionActualizada = 2,
    PersonaAgregada = 3,
    PersonaQuitada = 4,
    RolActualizado = 5,
    ContactoPrincipalActualizado = 6,
    FinanzasInquilinosActualizado = 7,
    NotaAgregada = 8,
    NotaEditada = 9,
    NotaEliminada = 10,
    IncidenciaRegistrada = 11,
    IncidenciaEditada = 12,
    IncidenciaEliminada = 13,
}

/// <summary>Evento de auditoría sobre una unidad (timeline de Actividad).</summary>
public class UnidadActividad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid UnidadId { get; set; }
    public Unidad Unidad { get; set; } = null!;

    public TipoActividad Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Detalle { get; set; }

    /// <summary>Usuario Identity que provocó el evento.</summary>
    public string? ActorUsuarioId { get; set; }
}
