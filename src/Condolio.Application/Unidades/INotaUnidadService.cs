using Condolio.Application.Common;

namespace Condolio.Application.Unidades;

public record NotaUnidadDto(
    Guid Id,
    string Texto,
    string Autor,
    string AutorUsuarioId,
    DateTime CreadoUtc,
    DateTime? ActualizadoUtc);

public record GuardarNotaDto(string Texto);

public interface INotaUnidadService
{
    Task<Result<IReadOnlyList<NotaUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default);
    Task<Result<NotaUnidadDto>> AgregarAsync(Guid consorcioId, Guid unidadId, GuardarNotaDto dto, CancellationToken ct = default);
    Task<Result<NotaUnidadDto>> EditarAsync(Guid consorcioId, Guid unidadId, Guid notaId, GuardarNotaDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, Guid notaId, CancellationToken ct = default);
}
