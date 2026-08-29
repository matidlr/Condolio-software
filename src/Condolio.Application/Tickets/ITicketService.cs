using Condolio.Application.Common;
using Condolio.Domain.Tickets;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Tickets;

public record TicketDto(
    Guid Id,
    int Numero,
    string? Titulo,
    string Descripcion,
    CategoriaTicket Categoria,
    EstadoTicket Estado,
    PrioridadTicket Prioridad,
    Guid? UnidadId,
    string? UnidadNombre,
    IReadOnlyList<string> Etiquetas,
    string? Ubicacion,
    DateTime? FechaLimite,
    string ReportadoPor,
    DateTime ReportadoUtc,
    string? AsignadoA,
    DateTime EstadoDesdeUtc,
    DateTime UltimaActividadUtc,
    bool Archivado);

public record TicketListaDto(
    IReadOnlyList<TicketDto> Tickets,
    int Activos,
    int Archivados);

public record TicketDetalleDto(
    TicketDto Ticket,
    IReadOnlyList<TicketComentarioDto> Comentarios);

public record TicketComentarioDto(string Texto, string Autor, DateTime FechaUtc, bool EsInterna);

public record UsuarioAsignableDto(string Id, string Nombre);

public record CrearTicketDto(
    string? Titulo,
    string Descripcion,
    CategoriaTicket Categoria,
    PrioridadTicket Prioridad,
    Guid? UnidadId,
    string? AsignadoAUsuarioId,
    DateTime? FechaLimite,
    List<string>? Etiquetas,
    string? Ubicacion);

public record ActualizarTicketDto(
    EstadoTicket Estado,
    PrioridadTicket Prioridad,
    string? AsignadoAUsuarioId);

public interface ITicketService
{
    Task<Result<TicketListaDto>> ListarAsync(Guid consorcioId, bool archivados, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UsuarioAsignableDto>>> AsignablesAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<TicketDetalleDto>> ObtenerAsync(Guid consorcioId, Guid ticketId, CancellationToken ct = default);
    Task<Result<TicketDto>> CrearAsync(Guid consorcioId, CrearTicketDto dto, CancellationToken ct = default);
    Task<Result<TicketDto>> ActualizarAsync(Guid consorcioId, Guid ticketId, ActualizarTicketDto dto, CancellationToken ct = default);
    Task<Result> ComentarAsync(Guid consorcioId, Guid ticketId, string texto, bool esInterna, CancellationToken ct = default);
    Task<Result> ArchivarAsync(Guid consorcioId, Guid ticketId, bool archivar, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid ticketId, CancellationToken ct = default);

    /// <summary>Crea un ticket a partir de una incidencia escalada. Devuelve el número asignado.</summary>
    Task<Result<int>> CrearDesdeIncidenciaAsync(
        Guid consorcioId, Guid unidadId, Guid incidenciaId,
        string? titulo, string descripcion, CategoriaIncidencia categoria, SeveridadIncidencia severidad,
        CancellationToken ct = default);
}
