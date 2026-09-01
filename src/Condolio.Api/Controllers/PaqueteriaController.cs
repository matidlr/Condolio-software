using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Paqueteria;
using Condolio.Domain.Paqueteria;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

/// <summary>Paquetería vista por la administración del consorcio.</summary>
[ApiController]
[Route("api/consorcios/{consorcioId:guid}/paquetes")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Operacion)]
public class PaqueteriaController : ApiControllerBase
{
    private readonly IPaqueteriaService _paquetes;

    public PaqueteriaController(IPaqueteriaService paquetes) => _paquetes = paquetes;

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _paquetes.ResumenAsync(consorcioId, ct));

    [HttpGet]
    public async Task<IActionResult> Listar(
        Guid consorcioId, [FromQuery] EstadoPaquete? estado, [FromQuery] string? q,
        [FromQuery] int anio, [FromQuery] int mes, CancellationToken ct) =>
        ToResult(await _paquetes.ListarAsync(consorcioId, estado, q, anio, mes, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalle(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _paquetes.ObtenerDetalleAsync(consorcioId, id, ct));

    [HttpPost("{id:guid}/entregar")]
    public async Task<IActionResult> Entregar(Guid consorcioId, Guid id, EntregarPaqueteDto dto, CancellationToken ct) =>
        ToResult(await _paquetes.EntregarAsync(consorcioId, id, dto ?? new EntregarPaqueteDto(null), "Administración", ct));

    [HttpPost("{id:guid}/recordatorio")]
    public async Task<IActionResult> Recordatorio(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _paquetes.EnviarRecordatorioAsync(consorcioId, id, ct));
}
