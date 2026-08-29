using Condolio.Application.Common;
using Condolio.Domain.Comunicaciones;

namespace Condolio.Application.Comunicaciones;

public record AnuncioDto(
    Guid Id,
    string? Titulo,
    string Cuerpo,
    CategoriaAnuncio Categoria,
    bool Fijado,
    DateTime PublicadoUtc,
    DateTime? EventoFechaUtc,
    string Autor,
    IReadOnlyList<Guid> ImagenesIds,
    int TotalLikes,
    int TotalComentarios,
    bool YoDiLike);

public record AnuncioComentarioDto(Guid Id, string Texto, string Autor, DateTime FechaUtc);
public record AnuncioLikeDto(string Nombre);

public record AnuncioDetalleDto(
    AnuncioDto Anuncio,
    IReadOnlyList<AnuncioComentarioDto> Comentarios,
    IReadOnlyList<AnuncioLikeDto> Likes);

public record LikeResultadoDto(int Total, bool YoDiLike, IReadOnlyList<AnuncioLikeDto> Likes);

public record AnuncioListaDto(
    IReadOnlyList<AnuncioDto> Anuncios,
    int Total,
    int General,
    int Mantenimiento,
    int Urgente,
    int Evento);

public record GuardarAnuncioDto(
    string? Titulo,
    string Cuerpo,
    CategoriaAnuncio Categoria,
    bool Fijado,
    DateTime? PublicadoUtc,
    DateTime? EventoFechaUtc,
    List<Guid>? ImagenesIds);

public interface IAnuncioService
{
    Task<Result<AnuncioListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<AnuncioDetalleDto>> ObtenerAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default);
    Task<Result<AnuncioDto>> CrearAsync(Guid consorcioId, GuardarAnuncioDto dto, CancellationToken ct = default);
    Task<Result<AnuncioDto>> ActualizarAsync(Guid consorcioId, Guid anuncioId, GuardarAnuncioDto dto, CancellationToken ct = default);
    Task<Result> FijarAsync(Guid consorcioId, Guid anuncioId, bool fijar, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default);
    Task<Result> ComentarAsync(Guid consorcioId, Guid anuncioId, string texto, CancellationToken ct = default);
    Task<Result> EditarComentarioAsync(Guid consorcioId, Guid anuncioId, Guid comentarioId, string texto, CancellationToken ct = default);
    Task<Result> EliminarComentarioAsync(Guid consorcioId, Guid anuncioId, Guid comentarioId, CancellationToken ct = default);
    Task<Result<LikeResultadoDto>> ToggleLikeAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default);
}
