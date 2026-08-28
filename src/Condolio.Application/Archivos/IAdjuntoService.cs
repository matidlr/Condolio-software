using Condolio.Application.Common;
using Condolio.Domain.Archivos;

namespace Condolio.Application.Archivos;

public record AdjuntoDto(
    Guid Id,
    string NombreArchivo,
    string ContentType,
    long Tamano,
    bool EsImagen,
    DateTime CreadoUtc);

public record NuevoAdjunto(
    string NombreArchivo,
    string ContentType,
    long Tamano,
    Stream Contenido);

public record ArchivoDescarga(string NombreArchivo, string ContentType, Stream Contenido);

public interface IAdjuntoService
{
    Task<Result<IReadOnlyList<AdjuntoDto>>> ListarAsync(TipoAdjuntoOwner ownerTipo, Guid ownerId, CancellationToken ct = default);
    Task<Result<AdjuntoDto>> SubirAsync(TipoAdjuntoOwner ownerTipo, Guid ownerId, NuevoAdjunto archivo, CancellationToken ct = default);
    Task<Result<ArchivoDescarga>> DescargarAsync(Guid adjuntoId, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid adjuntoId, CancellationToken ct = default);
}
