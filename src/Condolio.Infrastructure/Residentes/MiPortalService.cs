using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Encuestas;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Residentes;

public class MiPortalService : IMiPortalService
{
    private readonly CondolioDbContext _db;

    public MiPortalService(CondolioDbContext db) => _db = db;

    public async Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default)
    {
        var unidad = await _db.UnidadPersonas
            .IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new
            {
                p.UnidadId,
                UnidadNombre = p.Unidad.Nombre,
                p.Unidad.ConsorcioId,
                ConsorcioNombre = p.Unidad.Consorcio.Nombre,
                p.Unidad.Consorcio.Localidad,
            })
            .FirstOrDefaultAsync(ct);

        if (unidad is null)
            return Result<PortalCasaDto>.Fail("Todavía no tenés una unidad asignada.");

        var consorcioId = unidad.ConsorcioId;
        var ahora = DateTime.UtcNow;

        var votadas = await _db.VotosEncuesta.IgnoreQueryFilters()
            .Where(v => v.UsuarioId == usuarioId)
            .Select(v => v.EncuestaId)
            .Distinct()
            .ToListAsync(ct);

        var pendientes = await _db.Encuestas.IgnoreQueryFilters()
            .Where(e => e.ConsorcioId == consorcioId
                && e.Estado == EstadoEncuesta.Activa
                && !votadas.Contains(e.Id))
            .OrderBy(e => e.CierreUtc ?? DateTime.MaxValue)
            .Select(e => new { e.Id, e.Titulo, e.CierreUtc })
            .ToListAsync(ct);

        var encuestasPendientes = pendientes
            .Select(e => new EncuestaPendienteDto(
                e.Id, e.Titulo,
                e.CierreUtc is { } cierre ? Math.Max(0, (int)Math.Ceiling((cierre - ahora).TotalDays)) : (int?)null))
            .ToList();

        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId
                && r.Inicio >= ahora
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .OrderBy(r => r.Inicio)
            .Take(5)
            .Select(r => new ReservaResumenDto(r.Id, r.Amenidad.Nombre, r.Inicio, r.Estado.ToString()))
            .ToListAsync(ct);

        return Result<PortalCasaDto>.Ok(new PortalCasaDto(
            consorcioId, unidad.ConsorcioNombre, unidad.Localidad, unidad.UnidadNombre,
            encuestasPendientes, reservas,
            PaquetesPendientes: 0,
            NotificacionesNoLeidas: 0));
    }
}
