using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Amenidades;
using Condolio.Domain.Amenidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/reservas")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Operacion)]
public class ReservasController : ApiControllerBase
{
    private readonly IReservaService _reservas;

    public ReservasController(IReservaService reservas) => _reservas = reservas;

    public record EstadoBody(EstadoReserva Estado);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, [FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        ToResult(await _reservas.ListarAsync(consorcioId, desde, hasta, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, CrearReservaDto dto, CancellationToken ct) =>
        ToResult(await _reservas.CrearAsync(consorcioId, dto, ct));

    [HttpPost("{reservaId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid consorcioId, Guid reservaId, EstadoBody body, CancellationToken ct) =>
        ToResult(await _reservas.CambiarEstadoAsync(consorcioId, reservaId, body.Estado, ct));

    [HttpDelete("{reservaId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid reservaId, CancellationToken ct) =>
        ToResult(await _reservas.EliminarAsync(consorcioId, reservaId, ct));
}
