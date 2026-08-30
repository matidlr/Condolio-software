using Condolio.Application.Amenidades;
using Condolio.Application.Common;
using Condolio.Application.Comunicaciones;
using Condolio.Application.Documentos;
using Condolio.Application.Encuestas;

namespace Condolio.Application.Residentes;

public record CalendarioItemDto(
    Guid Id,
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    DateTime Inicio,
    DateTime Fin,
    bool TodoElDia,
    string Tipo,
    string? Categoria);

public record CrearEventoResidenteDto(
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    string Categoria,
    DateTime Inicio,
    DateTime Fin,
    bool TodoElDia);

public record EncuestaPendienteDto(Guid Id, string Titulo, int? DiasRestantes);

public record ReservaResumenDto(Guid Id, string Amenidad, DateTime Inicio, DateTime Fin, string Estado);

public record PortalCasaDto(
    Guid ConsorcioId,
    string ConsorcioNombre,
    string? Localidad,
    string UnidadNombre,
    IReadOnlyList<EncuestaPendienteDto> EncuestasPendientes,
    IReadOnlyList<ReservaResumenDto> ReservasProximas,
    int PaquetesPendientes,
    int NotificacionesNoLeidas);

public record IncidenciaResidenteDto(
    Guid Id,
    int Numero,
    string Titulo,
    string Descripcion,
    string Categoria,
    string Estado,
    string Prioridad,
    string? Ubicacion,
    DateTime CreadoUtc,
    DateTime UltimaActividadUtc);

public record IncidenciaMensajeDto(string Texto, string Autor, bool EsAdministracion, DateTime FechaUtc);

public record IncidenciaAdjuntoDto(Guid Id, string Nombre, string ContentType, bool EsImagen);

public record IncidenciaDetalleResidenteDto(
    IncidenciaResidenteDto Incidencia,
    IReadOnlyList<IncidenciaMensajeDto> Mensajes,
    IReadOnlyList<IncidenciaAdjuntoDto> Adjuntos);

public record ArchivoSubidaDto(string Nombre, string ContentType, long Tamano, Stream Contenido);

public record CrearIncidenciaResidenteDto(string Descripcion, string Categoria, IReadOnlyList<ArchivoSubidaDto>? Archivos = null);

public record SlotDto(DateTime Inicio, DateTime Fin);

public record MiReservaDto(
    Guid Id,
    Guid AmenidadId,
    string Amenidad,
    IReadOnlyList<Guid> ImagenesIds,
    DateTime Inicio,
    DateTime Fin,
    string Estado,
    string? Nota,
    DateTime CreadoUtc);

public record MisReservasDto(IReadOnlyList<MiReservaDto> Activas, IReadOnlyList<MiReservaDto> Previas);

public interface IMiPortalService
{
    Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AmenidadDto>>> AmenidadesAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<AmenidadDto>> AmenidadAsync(string usuarioId, Guid amenidadId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SlotDto>>> SlotsAsync(string usuarioId, Guid amenidadId, DateOnly fecha, CancellationToken ct = default);
    Task<Result<MiReservaDto>> SolicitarReservaAsync(string usuarioId, Guid amenidadId, DateTime inicio, DateTime fin, string? nota, CancellationToken ct = default);
    Task<Result<MisReservasDto>> MisReservasAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<MiReservaDto>> MiReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default);
    Task<Result> CancelarReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CalendarioItemDto>>> CalendarioAsync(string usuarioId, DateTime desde, DateTime hasta, CancellationToken ct = default);
    Task<Result<CalendarioItemDto>> CrearEventoAsync(string usuarioId, CrearEventoResidenteDto dto, CancellationToken ct = default);

    Task<Result<ContenidoDto>> DocumentosAsync(string usuarioId, Guid? carpetaId, CancellationToken ct = default);
    Task<Result<ArchivoDocumento>> DescargarDocumentoAsync(string usuarioId, Guid documentoId, bool registrarDescarga, CancellationToken ct = default);

    Task<Result<IReadOnlyList<EncuestaDto>>> EncuestasAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<EncuestaDetalleDto>> EncuestaAsync(string usuarioId, Guid encuestaId, CancellationToken ct = default);
    Task<Result<EncuestaDto>> VotarAsync(string usuarioId, Guid encuestaId, IReadOnlyList<Guid> opcionesIds, CancellationToken ct = default);

    Task<Result<IReadOnlyList<IncidenciaResidenteDto>>> IncidenciasAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<IncidenciaDetalleResidenteDto>> IncidenciaAsync(string usuarioId, Guid ticketId, CancellationToken ct = default);
    Task<Result<IncidenciaResidenteDto>> CrearIncidenciaAsync(string usuarioId, CrearIncidenciaResidenteDto dto, CancellationToken ct = default);
    Task<Result> ComentarIncidenciaAsync(string usuarioId, Guid ticketId, string texto, CancellationToken ct = default);
    Task<Result<ArchivoDocumento>> DescargarAdjuntoIncidenciaAsync(string usuarioId, Guid adjuntoId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AnuncioDto>>> MuroAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<AnuncioDetalleDto>> PublicacionAsync(string usuarioId, Guid anuncioId, CancellationToken ct = default);
    Task<Result<AnuncioDto>> PublicarAsync(string usuarioId, string cuerpo, IReadOnlyList<ArchivoSubidaDto>? imagenes, CancellationToken ct = default);
    Task<Result> ComentarMuroAsync(string usuarioId, Guid anuncioId, string texto, CancellationToken ct = default);
    Task<Result> EditarComentarioMuroAsync(string usuarioId, Guid anuncioId, Guid comentarioId, string texto, CancellationToken ct = default);
    Task<Result> EliminarComentarioMuroAsync(string usuarioId, Guid anuncioId, Guid comentarioId, CancellationToken ct = default);
    Task<Result<LikeResultadoDto>> ToggleLikeMuroAsync(string usuarioId, Guid anuncioId, CancellationToken ct = default);
    Task<Result<ArchivoDocumento>> DescargarAdjuntoMuroAsync(string usuarioId, Guid adjuntoId, CancellationToken ct = default);
}
