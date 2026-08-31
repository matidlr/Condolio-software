using Condolio.Application.Personal;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/personal")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class PersonalController : ApiControllerBase
{
    private readonly IPersonalService _personal;
    private readonly ICredencialCasetaService _credenciales;

    public PersonalController(IPersonalService personal, ICredencialCasetaService credenciales)
    {
        _personal = personal;
        _credenciales = credenciales;
    }

    // ---- Staff ----

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, [FromQuery] string? q, CancellationToken ct) =>
        ToResult(await _personal.ListarAsync(consorcioId, q, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, GuardarPersonalDto dto, CancellationToken ct) =>
        ToResult(await _personal.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid id, GuardarPersonalDto dto, CancellationToken ct) =>
        ToResult(await _personal.ActualizarAsync(consorcioId, id, dto, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _personal.EliminarAsync(consorcioId, id, ct));

    // ---- Credenciales de caseta ----

    public record NombreBody(string Nombre);

    [HttpGet("~/api/consorcios/{consorcioId:guid}/credenciales-caseta")]
    public async Task<IActionResult> Credenciales(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _credenciales.ListarAsync(consorcioId, ct));

    [HttpPost("~/api/consorcios/{consorcioId:guid}/credenciales-caseta")]
    public async Task<IActionResult> CrearCredencial(Guid consorcioId, NombreBody body, CancellationToken ct) =>
        ToResult(await _credenciales.CrearAsync(consorcioId, body.Nombre, ct));

    [HttpPut("~/api/consorcios/{consorcioId:guid}/credenciales-caseta/{id:guid}/nombre")]
    public async Task<IActionResult> RenombrarCredencial(Guid consorcioId, Guid id, NombreBody body, CancellationToken ct) =>
        ToResult(await _personal.ActualizarAsync(consorcioId, id,
            new GuardarPersonalDto(body.Nombre, "", Condolio.Domain.Personal.TipoPersonal.Seguridad, null), ct));

    [HttpPost("~/api/consorcios/{consorcioId:guid}/credenciales-caseta/{id:guid}/restablecer")]
    public async Task<IActionResult> RestablecerCredencial(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _credenciales.RegenerarClaveAsync(consorcioId, id, ct));

    [HttpPost("~/api/consorcios/{consorcioId:guid}/credenciales-caseta/{id:guid}/estado")]
    public async Task<IActionResult> EstadoCredencial(Guid consorcioId, Guid id, [FromQuery] bool activo, CancellationToken ct) =>
        ToResult(await _credenciales.CambiarEstadoAsync(consorcioId, id, activo, ct));

    [HttpDelete("~/api/consorcios/{consorcioId:guid}/credenciales-caseta/{id:guid}")]
    public async Task<IActionResult> EliminarCredencial(Guid consorcioId, Guid id, CancellationToken ct) =>
        ToResult(await _credenciales.EliminarAsync(consorcioId, id, ct));
}
