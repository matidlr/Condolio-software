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

    [HttpPost("reservas")]
    public async Task<IActionResult> Solicitar(SolicitarReservaBody body, CancellationToken ct) =>
        ToResult(await _portal.SolicitarReservaAsync(Uid, body.AmenidadId, body.Inicio, body.Fin, body.Nota, ct));

    [HttpDelete("reservas/{reservaId:guid}")]
    public async Task<IActionResult> Cancelar(Guid reservaId, CancellationToken ct) =>
        ToResult(await _portal.CancelarReservaAsync(Uid, reservaId, ct));
}
