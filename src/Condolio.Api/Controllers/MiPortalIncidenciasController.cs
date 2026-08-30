using System.Security.Claims;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal/incidencias")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalIncidenciasController : ApiControllerBase
{
    private readonly IMiPortalService _portal;

    public MiPortalIncidenciasController(IMiPortalService portal) => _portal = portal;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public record ComentarioBody(string Texto);
    public record RechazoBody(string? Motivo);

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        ToResult(await _portal.IncidenciasAsync(Uid, ct));

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> Obtener(Guid ticketId, CancellationToken ct) =>
        ToResult(await _portal.IncidenciaAsync(Uid, ticketId, ct));

    [HttpPost]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Crear(
        [FromForm] string descripcion,
        [FromForm] string categoria,
        [FromForm] List<IFormFile>? archivos,
        CancellationToken ct)
    {
        var subidas = new List<ArchivoSubidaDto>();
        foreach (var f in archivos ?? new())
        {
            if (f.Length == 0) continue;
            subidas.Add(new ArchivoSubidaDto(f.FileName, f.ContentType ?? "application/octet-stream", f.Length, f.OpenReadStream()));
        }
        try
        {
            return ToResult(await _portal.CrearIncidenciaAsync(Uid,
                new CrearIncidenciaResidenteDto(descripcion, categoria, subidas), ct));
        }
        finally
        {
            foreach (var s in subidas) await s.Contenido.DisposeAsync();
        }
    }

    [HttpPost("{ticketId:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid ticketId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _portal.ComentarIncidenciaAsync(Uid, ticketId, body.Texto, ct));

    [HttpPost("{ticketId:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid ticketId, CancellationToken ct) =>
        ToResult(await _portal.ConfirmarIncidenciaAsync(Uid, ticketId, ct));

    [HttpPost("{ticketId:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid ticketId, RechazoBody body, CancellationToken ct) =>
        ToResult(await _portal.RechazarIncidenciaAsync(Uid, ticketId, body?.Motivo, ct));

    [HttpGet("adjuntos/{adjuntoId:guid}")]
    public async Task<IActionResult> Adjunto(Guid adjuntoId, CancellationToken ct)
    {
        var r = await _portal.DescargarAdjuntoIncidenciaAsync(Uid, adjuntoId, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        return File(r.Valor!.Contenido, r.Valor.ContentType, r.Valor.Nombre);
    }
}
