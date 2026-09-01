using Condolio.Application.Consorcios;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Condolio.Api.Authorization;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class ConsorciosController : ApiControllerBase
{
    private readonly IConsorcioService _consorcios;
    private readonly IPreferenciasConsorcioService _preferencias;

    public ConsorciosController(IConsorcioService consorcios, IPreferenciasConsorcioService preferencias)
    {
        _consorcios = consorcios;
        _preferencias = preferencias;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _consorcios.ListarAsync(ct));

    [HttpGet("{id:guid}", Name = "ObtenerConsorcio")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        ToResult(await _consorcios.ObtenerAsync(id, ct));

    [HttpGet("{id:guid}/detalle")]
    [RequiereAdminGeneral]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct) =>
        ToResult(await _consorcios.DetalleAsync(id, ct));

    [HttpPost]
    [RequiereAdminGeneral]
    public async Task<IActionResult> Crear(CrearConsorcioDto dto, CancellationToken ct) =>
        ToResult(await _consorcios.CrearAsync(dto, ct));

    [HttpPut("{id:guid}")]
    [RequiereAdminGeneral]
    public async Task<IActionResult> Actualizar(Guid id, ActualizarConsorcioDto dto, CancellationToken ct) =>
        ToResult(await _consorcios.ActualizarAsync(id, dto, ct));

    [HttpDelete("{id:guid}")]
    [RequiereAdminGeneral]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) =>
        ToResult(await _consorcios.EliminarAsync(id, ct));

    [HttpGet("{id:guid}/preferencias")]
    [RequiereAdminGeneral]
    public async Task<IActionResult> Preferencias(Guid id, CancellationToken ct) =>
        ToResult(await _preferencias.ObtenerAsync(id, ct));

    [HttpPut("{id:guid}/preferencias")]
    [RequiereAdminGeneral]
    public async Task<IActionResult> GuardarPreferencias(Guid id, PreferenciasConsorcioDto dto, CancellationToken ct) =>
        ToResult(await _preferencias.GuardarAsync(id, dto, ct));
}
