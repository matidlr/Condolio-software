using Condolio.Application.Calendario;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/eventos")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class EventosController : ApiControllerBase
{
    private readonly IEventoService _eventos;

    public EventosController(IEventoService eventos) => _eventos = eventos;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, [FromQuery] DateTime desde, [FromQuery] DateTime hasta, CancellationToken ct) =>
        ToResult(await _eventos.ListarAsync(consorcioId, desde, hasta, ct));

    [HttpGet("{eventoId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid eventoId, CancellationToken ct) =>
        ToResult(await _eventos.ObtenerAsync(consorcioId, eventoId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, GuardarEventoDto dto, CancellationToken ct) =>
        ToResult(await _eventos.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{eventoId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid eventoId, GuardarEventoDto dto, CancellationToken ct) =>
        ToResult(await _eventos.ActualizarAsync(consorcioId, eventoId, dto, ct));

    [HttpDelete("{eventoId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid eventoId, CancellationToken ct) =>
        ToResult(await _eventos.EliminarAsync(consorcioId, eventoId, ct));
}
