using Condolio.Application.Comunicaciones;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/anuncios")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class AnunciosController : ApiControllerBase
{
    private readonly IAnuncioService _anuncios;

    public AnunciosController(IAnuncioService anuncios) => _anuncios = anuncios;

    public record FijarBody(bool Fijar);
    public record ComentarioBody(string Texto);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _anuncios.ListarAsync(consorcioId, ct));

    [HttpGet("{anuncioId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid anuncioId, CancellationToken ct) =>
        ToResult(await _anuncios.ObtenerAsync(consorcioId, anuncioId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, GuardarAnuncioDto dto, CancellationToken ct) =>
        ToResult(await _anuncios.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{anuncioId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid anuncioId, GuardarAnuncioDto dto, CancellationToken ct) =>
        ToResult(await _anuncios.ActualizarAsync(consorcioId, anuncioId, dto, ct));

    [HttpPost("{anuncioId:guid}/fijar")]
    public async Task<IActionResult> Fijar(Guid consorcioId, Guid anuncioId, FijarBody body, CancellationToken ct) =>
        ToResult(await _anuncios.FijarAsync(consorcioId, anuncioId, body.Fijar, ct));

    [HttpDelete("{anuncioId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid anuncioId, CancellationToken ct) =>
        ToResult(await _anuncios.EliminarAsync(consorcioId, anuncioId, ct));

    [HttpPost("{anuncioId:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid consorcioId, Guid anuncioId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _anuncios.ComentarAsync(consorcioId, anuncioId, body.Texto, ct));

    [HttpPut("{anuncioId:guid}/comentarios/{comentarioId:guid}")]
    public async Task<IActionResult> EditarComentario(Guid consorcioId, Guid anuncioId, Guid comentarioId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _anuncios.EditarComentarioAsync(consorcioId, anuncioId, comentarioId, body.Texto, ct));

    [HttpDelete("{anuncioId:guid}/comentarios/{comentarioId:guid}")]
    public async Task<IActionResult> EliminarComentario(Guid consorcioId, Guid anuncioId, Guid comentarioId, CancellationToken ct) =>
        ToResult(await _anuncios.EliminarComentarioAsync(consorcioId, anuncioId, comentarioId, ct));

    [HttpPost("{anuncioId:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid consorcioId, Guid anuncioId, CancellationToken ct) =>
        ToResult(await _anuncios.ToggleLikeAsync(consorcioId, anuncioId, ct));
}
