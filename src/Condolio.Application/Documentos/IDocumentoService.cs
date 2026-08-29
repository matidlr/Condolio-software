using Condolio.Application.Common;
using Condolio.Domain.Documentos;

namespace Condolio.Application.Documentos;

public record CarpetaDto(Guid Id, string Nombre, Guid? CarpetaPadreId, NivelAcceso Nivel, int Elementos);

public record DocumentoDto(
    Guid Id,
    string Nombre,
    string ContentType,
    long Tamano,
    Guid? CarpetaId,
    NivelAcceso Nivel,
    bool Destacado,
    DateTime CreadoUtc,
    DateTime? UltimoAccesoUtc,
    string SubidoPor);

public record ContenidoDto(
    Guid? CarpetaActualId,
    string? CarpetaActualNombre,
    IReadOnlyList<CarpetaDto> Carpetas,
    IReadOnlyList<DocumentoDto> Documentos,
    long AlmacenamientoUsado,
    long AlmacenamientoTotal);

public record CrearCarpetaDto(string Nombre, Guid? CarpetaPadreId, NivelAcceso Nivel);
public record NuevoDocumento(string Nombre, string ContentType, long Tamano, Stream Contenido, Guid? CarpetaId, NivelAcceso Nivel);
public record ActualizarDocumentoDto(string Nombre, NivelAcceso Nivel, Guid? CarpetaId);
public record ArchivoDocumento(string Nombre, string ContentType, Stream Contenido);

public interface IDocumentoService
{
    Task<Result<ContenidoDto>> ListarAsync(Guid consorcioId, Guid? carpetaId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentoDto>>> RecientesAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentoDto>>> DestacadosAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DocumentoDto>>> PorNivelAsync(Guid consorcioId, NivelAcceso nivel, CancellationToken ct = default);

    Task<Result<CarpetaDto>> CrearCarpetaAsync(Guid consorcioId, CrearCarpetaDto dto, CancellationToken ct = default);
    Task<Result> RenombrarCarpetaAsync(Guid consorcioId, Guid carpetaId, string nombre, CancellationToken ct = default);
    Task<Result> EliminarCarpetaAsync(Guid consorcioId, Guid carpetaId, CancellationToken ct = default);

    Task<Result<DocumentoDto>> SubirAsync(Guid consorcioId, NuevoDocumento archivo, CancellationToken ct = default);
    Task<Result<DocumentoDto>> ActualizarAsync(Guid consorcioId, Guid documentoId, ActualizarDocumentoDto dto, CancellationToken ct = default);
    Task<Result> DestacarAsync(Guid consorcioId, Guid documentoId, bool destacar, CancellationToken ct = default);
    Task<Result<ArchivoDocumento>> DescargarAsync(Guid consorcioId, Guid documentoId, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid documentoId, CancellationToken ct = default);
}
