using Condolio.Api.Authorization;
using Condolio.Application.Expensas;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

/// <summary>Setup de expensas: configuración, rubros, proveedores, empleados y gastos fijos.</summary>
[ApiController]
[Route("api/consorcios/{consorcioId:guid}/expensas")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Finanzas)]
public class ExpensasConfigController : ApiControllerBase
{
    private readonly IExpensasConfigService _svc;

    public ExpensasConfigController(IExpensasConfigService svc) => _svc = svc;

    // ---- Configuración ----

    [HttpGet("config")]
    public async Task<IActionResult> Config(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ObtenerConfigAsync(consorcioId, ct));

    [HttpPut("config")]
    public async Task<IActionResult> GuardarConfig(Guid consorcioId, GuardarConfigExpensasDto dto, CancellationToken ct) =>
        ToResult(await _svc.GuardarConfigAsync(consorcioId, dto, ct));

    [HttpPut("config/mercadopago")]
    public async Task<IActionResult> GuardarMercadoPago(Guid consorcioId, GuardarMercadoPagoDto dto, CancellationToken ct) =>
        ToResult(await _svc.GuardarMercadoPagoAsync(consorcioId, dto, ct));

    // ---- Rubros ----

    [HttpGet("rubros")]
    public async Task<IActionResult> Rubros(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ListarRubrosAsync(consorcioId, ct));

    [HttpPost("rubros")]
    public async Task<IActionResult> CrearRubro(Guid consorcioId, GuardarRubroDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearRubroAsync(consorcioId, dto, ct));

    [HttpPut("rubros/{id:guid}")]
    public async Task<IActionResult> ActualizarRubro(Guid consorcioId, Guid id, GuardarRubroDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarRubroAsync(consorcioId, id, dto, ct));

    [HttpDelete("rubros/{id:guid}")]
    public async Task<IActionResult> EliminarRubro(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _svc.EliminarRubroAsync(consorcioId, id, ct));

    // ---- Proveedores ----

    [HttpGet("proveedores")]
    public async Task<IActionResult> Proveedores(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ListarProveedoresAsync(consorcioId, ct));

    [HttpPost("proveedores")]
    public async Task<IActionResult> CrearProveedor(Guid consorcioId, GuardarProveedorDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearProveedorAsync(consorcioId, dto, ct));

    [HttpPut("proveedores/{id:guid}")]
    public async Task<IActionResult> ActualizarProveedor(Guid consorcioId, Guid id, GuardarProveedorDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarProveedorAsync(consorcioId, id, dto, ct));

    public record EstadoBody(bool Activo);

    [HttpPost("proveedores/{id:guid}/estado")]
    public async Task<IActionResult> EstadoProveedor(Guid consorcioId, Guid id, EstadoBody body, CancellationToken ct) =>
        ToResult(await _svc.CambiarEstadoProveedorAsync(consorcioId, id, body.Activo, ct));

    public record RecomendadoBody(bool Recomendado);

    [HttpPost("proveedores/{id:guid}/recomendado")]
    public async Task<IActionResult> RecomendarProveedor(Guid consorcioId, Guid id, RecomendadoBody body, CancellationToken ct) =>
        ToResult(await _svc.CambiarRecomendadoProveedorAsync(consorcioId, id, body.Recomendado, ct));

    // ---- Gastos fijos + empleados ----

    [HttpGet("gastos-fijos")]
    public async Task<IActionResult> GastosFijos(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ResumenGastosFijosAsync(consorcioId, ct));

    [HttpPost("empleados")]
    public async Task<IActionResult> CrearEmpleado(Guid consorcioId, GuardarEmpleadoDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearEmpleadoAsync(consorcioId, dto, ct));

    [HttpPut("empleados/{id:guid}")]
    public async Task<IActionResult> ActualizarEmpleado(Guid consorcioId, Guid id, GuardarEmpleadoDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarEmpleadoAsync(consorcioId, id, dto, ct));

    [HttpPost("empleados/{id:guid}/estado")]
    public async Task<IActionResult> EstadoEmpleado(Guid consorcioId, Guid id, EstadoBody body, CancellationToken ct) =>
        ToResult(await _svc.CambiarEstadoEmpleadoAsync(consorcioId, id, body.Activo, ct));

    [HttpPost("gastos-fijos")]
    public async Task<IActionResult> CrearGastoFijo(Guid consorcioId, GuardarGastoFijoDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearGastoFijoAsync(consorcioId, dto, ct));

    [HttpPut("gastos-fijos/{id:guid}")]
    public async Task<IActionResult> ActualizarGastoFijo(Guid consorcioId, Guid id, GuardarGastoFijoDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarGastoFijoAsync(consorcioId, id, dto, ct));

    [HttpPost("gastos-fijos/{id:guid}/estado")]
    public async Task<IActionResult> EstadoGastoFijo(Guid consorcioId, Guid id, EstadoBody body, CancellationToken ct) =>
        ToResult(await _svc.CambiarEstadoGastoFijoAsync(consorcioId, id, body.Activo, ct));

    // ---- Expensas extraordinarias ----

    [HttpGet("extraordinarias")]
    public async Task<IActionResult> Extraordinarias(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ListarExtraordinariasAsync(consorcioId, ct));

    [HttpPost("extraordinarias")]
    public async Task<IActionResult> CrearExtraordinaria(Guid consorcioId, GuardarExtraordinariaDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearExtraordinariaAsync(consorcioId, dto, ct));

    [HttpPut("extraordinarias/{id:guid}")]
    public async Task<IActionResult> ActualizarExtraordinaria(Guid consorcioId, Guid id, GuardarExtraordinariaDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarExtraordinariaAsync(consorcioId, id, dto, ct));

    public record EstadoExtraordinariaBody(Condolio.Domain.Expensas.EstadoExtraordinaria Estado);

    [HttpPost("extraordinarias/{id:guid}/estado")]
    public async Task<IActionResult> EstadoExtraordinaria(Guid consorcioId, Guid id, EstadoExtraordinariaBody body, CancellationToken ct) =>
        ToResult(await _svc.CambiarEstadoExtraordinariaAsync(consorcioId, id, body.Estado, ct));
}
