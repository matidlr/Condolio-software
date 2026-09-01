using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Unidades;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/unidades")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Operacion)]
public class UnidadesController : ApiControllerBase
{
    private readonly IUnidadService _unidades;

    public UnidadesController(IUnidadService unidades) => _unidades = unidades;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _unidades.ListarAsync(consorcioId, ct));

    [HttpGet("{unidadId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        ToResult(await _unidades.ObtenerAsync(consorcioId, unidadId, ct));

    [HttpGet("{unidadId:guid}/actividad")]
    public async Task<IActionResult> Actividad(Guid consorcioId, Guid unidadId, [FromServices] IActividadUnidadService actividad, CancellationToken ct) =>
        ToResult(await actividad.ListarAsync(consorcioId, unidadId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, CrearUnidadDto dto, CancellationToken ct) =>
        ToResult(await _unidades.CrearAsync(consorcioId, dto, ct));

    [HttpPost("lote")]
    public async Task<IActionResult> CrearLote(Guid consorcioId, CrearUnidadesLoteDto dto, CancellationToken ct) =>
        ToResult(await _unidades.CrearLoteAsync(consorcioId, dto, ct));

    [HttpPost("importar")]
    public async Task<IActionResult> Importar(Guid consorcioId, ImportarUnidadesDto dto, CancellationToken ct) =>
        ToResult(await _unidades.ImportarAsync(consorcioId, dto, ct));

    [HttpPut("masivo")]
    public async Task<IActionResult> EditarMasivo(Guid consorcioId, EdicionMasivaDto dto, CancellationToken ct) =>
        ToResult(await _unidades.EditarMasivoAsync(consorcioId, dto, ct));

    [HttpPut("{unidadId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid unidadId, ActualizarUnidadDto dto, CancellationToken ct) =>
        ToResult(await _unidades.ActualizarAsync(consorcioId, unidadId, dto, ct));

    [HttpDelete("{unidadId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        ToResult(await _unidades.EliminarAsync(consorcioId, unidadId, ct));

    // ---- Ocupación y personas ----

    public record CambiarOcupacionBody(TipoOcupacion Ocupacion);
    public record CambiarRolBody(RolUnidad Rol);
    public record InquilinosFinanzasBody(bool Permitir);

    [HttpPut("{unidadId:guid}/ocupacion")]
    public async Task<IActionResult> CambiarOcupacion(Guid consorcioId, Guid unidadId, CambiarOcupacionBody body, CancellationToken ct) =>
        ToResult(await _unidades.CambiarOcupacionAsync(consorcioId, unidadId, body.Ocupacion, ct));

    [HttpPut("{unidadId:guid}/inquilinos-finanzas")]
    public async Task<IActionResult> InquilinosFinanzas(Guid consorcioId, Guid unidadId, InquilinosFinanzasBody body, CancellationToken ct) =>
        ToResult(await _unidades.CambiarInquilinosVenFinanzasAsync(consorcioId, unidadId, body.Permitir, ct));

    [HttpPost("{unidadId:guid}/personas")]
    public async Task<IActionResult> AgregarPersona(Guid consorcioId, Guid unidadId, GuardarPersonaDto dto, CancellationToken ct) =>
        ToResult(await _unidades.AgregarPersonaAsync(consorcioId, unidadId, dto, ct));

    [HttpPut("{unidadId:guid}/personas/{personaId:guid}/principal")]
    public async Task<IActionResult> MarcarPrincipal(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct) =>
        ToResult(await _unidades.MarcarContactoPrincipalAsync(consorcioId, unidadId, personaId, ct));

    [HttpPut("{unidadId:guid}/personas/{personaId:guid}/rol")]
    public async Task<IActionResult> CambiarRolPersona(Guid consorcioId, Guid unidadId, Guid personaId, CambiarRolBody body, CancellationToken ct) =>
        ToResult(await _unidades.CambiarRolPersonaAsync(consorcioId, unidadId, personaId, body.Rol, ct));

    [HttpDelete("{unidadId:guid}/personas/{personaId:guid}")]
    public async Task<IActionResult> EliminarPersona(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct) =>
        ToResult(await _unidades.EliminarPersonaAsync(consorcioId, unidadId, personaId, ct));
}
