using System.Security.Claims;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalAmenidadesController : ApiControllerBase
{
    private readonly IMiPortalService _portal;

    public MiPortalAmenidadesController(IMiPortalService portal) => _portal = portal;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public record SolicitarReservaBody(Guid AmenidadId, DateTime Inicio, DateTime Fin, string? Nota);
    public record CrearEventoBody(string Titulo, string? Descripcion, string? Ubicacion, string Categoria, DateTime Inicio, DateTime Fin, bool TodoElDia);

    [HttpGet("amenidades")]
    public async Task<IActionResult> Amenidades(CancellationToken ct) =>
        ToResult(await _portal.AmenidadesAsync(Uid, ct));

    [HttpGet("amenidades/{amenidadId:guid}")]
    public async Task<IActionResult> Amenidad(Guid amenidadId, CancellationToken ct) =>
        ToResult(await _portal.AmenidadAsync(Uid, amenidadId, ct));

    [HttpGet("amenidades/{amenidadId:guid}/slots")]
    public async Task<IActionResult> Slots(Guid amenidadId, [FromQuery] DateOnly fecha, CancellationToken ct) =>
        ToResult(await _portal.SlotsAsync(Uid, amenidadId, fecha, ct));

    [HttpGet("reservas")]
    public async Task<IActionResult> MisReservas(CancellationToken ct) =>
        ToResult(await _portal.MisReservasAsync(Uid, ct));

    [HttpGet("reservas/{reservaId:guid}")]
    public async Task<IActionResult> MiReserva(Guid reservaId, CancellationToken ct) =>
        ToResult(await _portal.MiReservaAsync(Uid, reservaId, ct));

    [HttpGet("calendario")]
    public async Task<IActionResult> Calendario([FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        ToResult(await _portal.CalendarioAsync(Uid, desde, hasta, ct));

    [HttpPost("eventos")]
    public async Task<IActionResult> CrearEvento(CrearEventoBody b, CancellationToken ct) =>
        ToResult(await _portal.CrearEventoAsync(Uid,
            new Application.Residentes.CrearEventoResidenteDto(b.Titulo, b.Descripcion, b.Ubicacion, b.Categoria, b.Inicio, b.Fin, b.TodoElDia), ct));

    [HttpGet("documentos")]
    public async Task<IActionResult> Documentos([FromQuery] Guid? carpetaId, CancellationToken ct) =>
        ToResult(await _portal.DocumentosAsync(Uid, carpetaId, ct));

    [HttpGet("documentos/{documentoId:guid}/descargar")]
    public async Task<IActionResult> DescargarDocumento(Guid documentoId, [FromQuery] bool descarga, CancellationToken ct)
    {
        var r = await _portal.DescargarDocumentoAsync(Uid, documentoId, descarga, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        return File(r.Valor!.Contenido, r.Valor.ContentType, r.Valor.Nombre);
    }

    public record VotarBody(List<Guid> OpcionesIds);

    [HttpGet("encuestas")]
    public async Task<IActionResult> Encuestas(CancellationToken ct) =>
        ToResult(await _portal.EncuestasAsync(Uid, ct));

    [HttpGet("encuestas/{encuestaId:guid}")]
    public async Task<IActionResult> Encuesta(Guid encuestaId, CancellationToken ct) =>
        ToResult(await _portal.EncuestaAsync(Uid, encuestaId, ct));

    [HttpPost("encuestas/{encuestaId:guid}/votar")]
    public async Task<IActionResult> Votar(Guid encuestaId, VotarBody body, CancellationToken ct) =>
        ToResult(await _portal.VotarAsync(Uid, encuestaId, body.OpcionesIds ?? new(), ct));

    [HttpPost("reservas")]
    public async Task<IActionResult> Solicitar(SolicitarReservaBody body, CancellationToken ct) =>
        ToResult(await _portal.SolicitarReservaAsync(Uid, body.AmenidadId, body.Inicio, body.Fin, body.Nota, ct));

    [HttpDelete("reservas/{reservaId:guid}")]
    public async Task<IActionResult> Cancelar(Guid reservaId, CancellationToken ct) =>
        ToResult(await _portal.CancelarReservaAsync(Uid, reservaId, ct));
}
