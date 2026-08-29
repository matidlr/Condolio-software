using Condolio.Application.Tickets;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/tickets")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class TicketsController : ApiControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets) => _tickets = tickets;

    public record ComentarioBody(string Texto);
    public record ArchivarBody(bool Archivar);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, [FromQuery] bool archivados, CancellationToken ct) =>
        ToResult(await _tickets.ListarAsync(consorcioId, archivados, ct));

    [HttpGet("asignables")]
    public async Task<IActionResult> Asignables(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _tickets.AsignablesAsync(consorcioId, ct));

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid ticketId, CancellationToken ct) =>
        ToResult(await _tickets.ObtenerAsync(consorcioId, ticketId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, CrearTicketDto dto, CancellationToken ct) =>
        ToResult(await _tickets.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{ticketId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid ticketId, ActualizarTicketDto dto, CancellationToken ct) =>
        ToResult(await _tickets.ActualizarAsync(consorcioId, ticketId, dto, ct));

    [HttpPost("{ticketId:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid consorcioId, Guid ticketId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _tickets.ComentarAsync(consorcioId, ticketId, body.Texto, ct));

    [HttpPost("{ticketId:guid}/archivar")]
    public async Task<IActionResult> Archivar(Guid consorcioId, Guid ticketId, ArchivarBody body, CancellationToken ct) =>
        ToResult(await _tickets.ArchivarAsync(consorcioId, ticketId, body.Archivar, ct));

    [HttpDelete("{ticketId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid ticketId, CancellationToken ct) =>
        ToResult(await _tickets.EliminarAsync(consorcioId, ticketId, ct));
}
