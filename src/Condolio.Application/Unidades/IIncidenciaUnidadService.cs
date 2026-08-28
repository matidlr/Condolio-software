using Condolio.Application.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Unidades;

public record IncidenciaUnidadDto(
    Guid Id,
    string? Titulo,
    string Descripcion,
    CategoriaIncidencia Categoria,
    SeveridadIncidencia Severidad,
    DateTime FechaEvento,
    IReadOnlyList<string> Etiquetas,
    string Autor,
    DateTime CreadoUtc,
    DateTime? ActualizadoUtc,
    DateTime? EscaladaUtc);

public record IncidenciaHistorialDto(string Tipo, string Texto, string Autor, DateTime FechaUtc);

public record IncidenciaDetalleDto(IncidenciaUnidadDto Incidencia, IReadOnlyList<IncidenciaHistorialDto> Historial);

public record GuardarIncidenciaDto(
    string? Titulo,
    string Descripcion,
    CategoriaIncidencia Categoria,
    SeveridadIncidencia Severidad,
    DateTime? FechaEvento,
    List<string>? Etiquetas);

public interface IIncidenciaUnidadService
{
    Task<Result<IReadOnlyList<IncidenciaUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default);
    Task<Result<IncidenciaDetalleDto>> ObtenerAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default);
    Task<Result<IncidenciaUnidadDto>> RegistrarAsync(Guid consorcioId, Guid unidadId, GuardarIncidenciaDto dto, CancellationToken ct = default);
    Task<Result<IncidenciaUnidadDto>> EditarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, GuardarIncidenciaDto dto, CancellationToken ct = default);
    Task<Result> AgregarComentarioAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, string texto, CancellationToken ct = default);
    Task<Result> EscalarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default);
}
