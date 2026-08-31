using Condolio.Application.Common;

namespace Condolio.Application.Personal;

public record CredencialCasetaDto(
    Guid Id,
    string Nombre,
    string Email,
    bool Activo,
    DateTime CreadoUtc);

public record CredencialesCasetaListaDto(
    IReadOnlyList<CredencialCasetaDto> Dispositivos,
    int Total,
    int Activos,
    DateTime? UltimoAgregadoUtc,
    string? UltimoAgregadoNombre);

/// <summary>Credenciales recién generadas — se muestran una sola vez.</summary>
public record CredencialGeneradaDto(CredencialCasetaDto Dispositivo, string Email, string Password);

public interface ICredencialCasetaService
{
    Task<Result<CredencialesCasetaListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<CredencialGeneradaDto>> CrearAsync(Guid consorcioId, string nombre, CancellationToken ct = default);
    Task<Result<CredencialGeneradaDto>> RegenerarClaveAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
    Task<Result> CambiarEstadoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
}
