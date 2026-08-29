using Condolio.Domain.Common;

namespace Condolio.Domain.Amenidades;

public enum EstadoReserva
{
    Pendiente = 0,
    Confirmada = 1,
    Rechazada = 2,
    Cancelada = 3,
}

/// <summary>Reserva de una amenidad para una franja horaria.</summary>
public class Reserva : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }
    public Guid AmenidadId { get; set; }
    public Amenidad Amenidad { get; set; } = null!;

    public Guid? UnidadId { get; set; }
    public string SolicitanteUsuarioId { get; set; } = string.Empty;
    public string SolicitanteNombre { get; set; } = string.Empty;

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
    public decimal? Importe { get; set; }
    public string? Nota { get; set; }
    public DateTime? ResueltaUtc { get; set; }
}
