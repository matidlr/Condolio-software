using Condolio.Application.Encuestas;
using Condolio.Domain.Encuestas;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/encuestas")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class EncuestasController : ApiControllerBase
{
    private readonly IEncuestaService _encuestas;

    public EncuestasController(IEncuestaService encuestas) => _encuestas = encuestas;

    public record EstadoBody(EstadoEncuesta Estado);
    public record VotarBody(List<Guid> OpcionesIds);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _encuestas.ListarAsync(consorcioId, ct));

    [HttpGet("{encuestaId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid encuestaId, CancellationToken ct) =>
        ToResult(await _encuestas.ObtenerAsync(consorcioId, encuestaId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, GuardarEncuestaDto dto, CancellationToken ct) =>
        ToResult(await _encuestas.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{encuestaId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid encuestaId, GuardarEncuestaDto dto, CancellationToken ct) =>
        ToResult(await _encuestas.ActualizarAsync(consorcioId, encuestaId, dto, ct));

    [HttpPost("{encuestaId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid consorcioId, Guid encuestaId, EstadoBody body, CancellationToken ct) =>
        ToResult(await _encuestas.CambiarEstadoAsync(consorcioId, encuestaId, body.Estado, ct));

    [HttpPost("{encuestaId:guid}/votar")]
    public async Task<IActionResult> Votar(Guid consorcioId, Guid encuestaId, VotarBody body, CancellationToken ct) =>
        ToResult(await _encuestas.VotarAsync(consorcioId, encuestaId, body.OpcionesIds ?? new(), ct));

    [HttpDelete("{encuestaId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid encuestaId, CancellationToken ct) =>
        ToResult(await _encuestas.EliminarAsync(consorcioId, encuestaId, ct));
}
