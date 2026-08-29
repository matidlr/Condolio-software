using Condolio.Domain.Common;

namespace Condolio.Domain.Amenidades;

/// <summary>Instalación o espacio común del consorcio (piscina, SUM, gimnasio, cochera de visitas...).</summary>
public class Amenidad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    /// <summary>
    /// Ids de <see cref="Condolio.Domain.Archivos.Adjunto"/> (imágenes) separados por coma y en orden.
    /// El primero es la portada.
    /// </summary>
    public string? ImagenesIds { get; set; }

    // ---- Configuración de reservaciones ----
    public bool Reservable { get; set; }
    /// <summary>Duración de cada bloque de reserva, en minutos (30, 60, 120...).</summary>
    public int IntervaloMinutos { get; set; } = 60;
    /// <summary>false = límite de reservas simultáneas por residente; true = límite mensual por unidad.</summary>
    public bool LimiteMensual { get; set; }
    /// <summary>Cantidad máxima según <see cref="LimiteMensual"/>. 0 = ilimitadas.</summary>
    public int MaxReservasPorUnidad { get; set; } = 1;

    public bool TieneCosto { get; set; }
    public decimal? Tarifa { get; set; }

    public bool RequiereAprobacion { get; set; }

    /// <summary>Fecha a partir de la cual se puede reservar.</summary>
    public DateOnly? ReservableDesde { get; set; }
    /// <summary>Fechas bloqueadas (yyyy-MM-dd) separadas por coma.</summary>
    public string? DiasBloqueados { get; set; }
    public string? MensajeReserva { get; set; }

    public List<AmenidadHorario> Horarios { get; set; } = new();
}

/// <summary>Franja horaria disponible de una amenidad para un día de la semana.</summary>
public class AmenidadHorario : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid AmenidadId { get; set; }
    public Amenidad Amenidad { get; set; } = null!;

    public DayOfWeek Dia { get; set; }
    public bool Cerrado { get; set; }
    /// <summary>Minutos desde medianoche (0-1440).</summary>
    public int AbreMin { get; set; } = 8 * 60;
    public int CierraMin { get; set; } = 22 * 60;
}
