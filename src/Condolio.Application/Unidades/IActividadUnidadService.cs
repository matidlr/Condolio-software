using Condolio.Application.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Unidades;

public record ActividadUnidadDto(
    Guid Id,
    TipoActividad Tipo,
    string Titulo,
    string? Detalle,
    string Actor,
    DateTime CreadoUtc);

public interface IActividadUnidadService
{
    /// <summary>Registra un evento de auditoría. El actor sale del contexto de tenant.</summary>
    Task RegistrarAsync(Guid unidadId, TipoActividad tipo, string titulo, string? detalle, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ActividadUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default);
}
