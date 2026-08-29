using Condolio.Application.Common;
using Condolio.Domain.Encuestas;

namespace Condolio.Application.Encuestas;

public record OpcionResultadoDto(Guid Id, string Texto, int Votos, double Porcentaje, bool YoVote);

public record EncuestaDto(
    Guid Id,
    string Titulo,
    string Descripcion,
    CategoriaEncuesta Categoria,
    EstadoEncuesta Estado,
    bool MultiplesOpciones,
    bool Anonima,
    DateTime? PublicadaUtc,
    DateTime? CierreUtc,
    string Autor,
    int TotalVotos,
    int TotalVotantes,
    bool YoVote,
    IReadOnlyList<OpcionResultadoDto> Opciones);

public record VotanteDto(string Nombre, string Opcion, DateTime FechaUtc);

public record EncuestaDetalleDto(EncuestaDto Encuesta, IReadOnlyList<VotanteDto> Votantes);

public record EstadisticasEncuestasDto(int Total, int Activas, int Borradores, int Cerradas, int TotalVotos);

public record EncuestaListaDto(
    IReadOnlyList<EncuestaDto> Encuestas,
    EstadisticasEncuestasDto Estadisticas,
    int General,
    int Mantenimiento,
    int Evento);

public record GuardarEncuestaDto(
    string Titulo,
    string Descripcion,
    CategoriaEncuesta Categoria,
    IReadOnlyList<string> Opciones,
    bool MultiplesOpciones,
    bool Anonima,
    DateTime? CierreUtc,
    bool Publicar);

public interface IEncuestaService
{
    Task<Result<EncuestaListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<EncuestaDetalleDto>> ObtenerAsync(Guid consorcioId, Guid encuestaId, CancellationToken ct = default);
    Task<Result<EncuestaDto>> CrearAsync(Guid consorcioId, GuardarEncuestaDto dto, CancellationToken ct = default);
    Task<Result<EncuestaDto>> ActualizarAsync(Guid consorcioId, Guid encuestaId, GuardarEncuestaDto dto, CancellationToken ct = default);
    Task<Result<EncuestaDto>> CambiarEstadoAsync(Guid consorcioId, Guid encuestaId, EstadoEncuesta estado, CancellationToken ct = default);
    Task<Result<EncuestaDto>> VotarAsync(Guid consorcioId, Guid encuestaId, IReadOnlyList<Guid> opcionesIds, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid encuestaId, CancellationToken ct = default);
}
