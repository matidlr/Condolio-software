using Condolio.Application.Common;
using Condolio.Application.Panel;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Tickets;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Panel;

public class PanelService : IPanelService
{
    private readonly CondolioDbContext _db;

    public PanelService(CondolioDbContext db) => _db = db;

    public async Task<Result<PanelResumenDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var nombre = await _db.Consorcios.Where(c => c.Id == consorcioId).Select(c => c.Nombre).FirstOrDefaultAsync(ct);
        if (nombre is null) return Result<PanelResumenDto>.Fail("Consorcio no encontrado.");

        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var inicioSemana = ahora.Date.AddDays(-(int)ahora.DayOfWeek);
        var finSemana = inicioSemana.AddDays(7);
        var finDia = ahora.Date.AddDays(1);

        var unidades = await _db.Unidades.CountAsync(u => u.ConsorcioId == consorcioId, ct);

        var personas = await _db.UnidadPersonas
            .Where(p => p.Unidad.ConsorcioId == consorcioId && p.Rol != RolUnidad.Gestor)
            .Select(p => new { p.Email, p.UsuarioId })
            .ToListAsync(ct);
        var residentes = personas.Where(p => !string.IsNullOrEmpty(p.Email)).Select(p => p.Email!).Distinct().Count()
                       + personas.Count(p => string.IsNullOrEmpty(p.Email));
        var residentesActivos = personas.Where(p => !string.IsNullOrEmpty(p.UsuarioId)).Select(p => p.UsuarioId!).Distinct().Count();

        var publicacionesMes = await _db.Anuncios
            .CountAsync(a => a.ConsorcioId == consorcioId && a.PublicadoUtc >= inicioMes, ct);

        var ticketsAbiertos = await _db.Tickets
            .CountAsync(t => t.ConsorcioId == consorcioId && t.ArchivadoUtc == null && t.Estado != EstadoTicket.Resuelto, ct);

        var reservasSemana = await _db.Reservas
            .CountAsync(r => r.ConsorcioId == consorcioId
                && r.Inicio >= inicioSemana && r.Inicio < finSemana
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada), ct);

        return Result<PanelResumenDto>.Ok(new PanelResumenDto(
            nombre, unidades, residentes, residentesActivos, publicacionesMes,
            ticketsAbiertos, reservasSemana,
            PaquetesPendientes: 0, PagosMes: 0m, EntradasHoy: 0, CodigosQrActivos: 0));
    }
}
