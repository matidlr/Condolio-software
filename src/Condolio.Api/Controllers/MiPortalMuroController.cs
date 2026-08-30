using System.Security.Claims;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal/muro")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalMuroController : ApiControllerBase
{
    private readonly IMiPortalService _portal;

    public MiPortalMuroController(IMiPortalService portal) => _portal = portal;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public record ComentarioBody(string Texto);

    [HttpGet]
    public async Task<IActionResult> Feed(CancellationToken ct) =>
        ToResult(await _portal.MuroAsync(Uid, ct));

    [HttpGet("{anuncioId:guid}")]
    public async Task<IActionResult> Publicacion(Guid anuncioId, CancellationToken ct) =>
        ToResult(await _portal.PublicacionAsync(Uid, anuncioId, ct));

    [HttpPost]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> Publicar(
        [FromForm] string? cuerpo,
        [FromForm] List<IFormFile>? imagenes,
        CancellationToken ct)
    {
        var subidas = new List<ArchivoSubidaDto>();
        foreach (var f in imagenes ?? new())
        {
            if (f.Length == 0) continue;
            subidas.Add(new ArchivoSubidaDto(f.FileName, f.ContentType ?? "image/jpeg", f.Length, f.OpenReadStream()));
        }
        try
        {
            return ToResult(await _portal.PublicarAsync(Uid, cuerpo ?? string.Empty, subidas, ct));
        }
        finally
        {
            foreach (var s in subidas) await s.Contenido.DisposeAsync();
        }
    }

    [HttpPost("{anuncioId:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid anuncioId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _portal.ComentarMuroAsync(Uid, anuncioId, body.Texto, ct));

    [HttpPut("{anuncioId:guid}/comentarios/{comentarioId:guid}")]
    public async Task<IActionResult> Editar(Guid anuncioId, Guid comentarioId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _portal.EditarComentarioMuroAsync(Uid, anuncioId, comentarioId, body.Texto, ct));

    [HttpDelete("{anuncioId:guid}/comentarios/{comentarioId:guid}")]
    public async Task<IActionResult> Eliminar(Guid anuncioId, Guid comentarioId, CancellationToken ct) =>
        ToResult(await _portal.EliminarComentarioMuroAsync(Uid, anuncioId, comentarioId, ct));

    [HttpPost("{anuncioId:guid}/like")]
    public async Task<IActionResult> Like(Guid anuncioId, CancellationToken ct) =>
        ToResult(await _portal.ToggleLikeMuroAsync(Uid, anuncioId, ct));

    [HttpGet("adjuntos/{adjuntoId:guid}")]
    public async Task<IActionResult> Adjunto(Guid adjuntoId, CancellationToken ct)
    {
        var r = await _portal.DescargarAdjuntoMuroAsync(Uid, adjuntoId, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        return File(r.Valor!.Contenido, r.Valor.ContentType, r.Valor.Nombre);
    }
}
