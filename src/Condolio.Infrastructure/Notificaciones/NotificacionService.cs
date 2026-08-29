using Condolio.Application.Common;
using Condolio.Application.Notificaciones;
using Condolio.Domain.Notificaciones;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Condolio.Infrastructure.Notificaciones;

public class NotificacionService : INotificacionService
{
    private const int MaxLista = 100;

    private readonly CondolioDbContext _db;
    private readonly ILogger<NotificacionService> _log;

    public NotificacionService(CondolioDbContext db, ILogger<NotificacionService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<Result<NotificacionListaDto>> ListarAsync(
        Guid consorcioId, bool soloNoLeidas = false, TipoNotificacion? tipo = null, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<NotificacionListaDto>.Fail("Consorcio no encontrado.");

        var baseQuery = _db.Notificaciones.Where(n => n.ConsorcioId == consorcioId);

        var total = await baseQuery.CountAsync(ct);
        var noLeidas = await baseQuery.CountAsync(n => n.LeidaUtc == null, ct);

        var q = baseQuery;
        if (soloNoLeidas) q = q.Where(n => n.LeidaUtc == null);
        if (tipo is { } t) q = q.Where(n => n.Tipo == t);

        var lista = await q
            .OrderByDescending(n => n.CreadoUtc)
            .Take(MaxLista)
            .Select(n => new NotificacionDto(
                n.Id, n.Tipo, n.Titulo, n.Cuerpo, n.Enlace, n.LeidaUtc != null, n.CreadoUtc))
            .ToListAsync(ct);

        return Result<NotificacionListaDto>.Ok(new NotificacionListaDto(lista, total, noLeidas));
    }

    public async Task<Result<NotificacionResumenDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var total = await _db.Notificaciones.CountAsync(n => n.ConsorcioId == consorcioId, ct);
        var noLeidas = await _db.Notificaciones.CountAsync(n => n.ConsorcioId == consorcioId && n.LeidaUtc == null, ct);
        return Result<NotificacionResumenDto>.Ok(new NotificacionResumenDto(total, noLeidas));
    }

    public async Task<Result> MarcarLeidaAsync(Guid consorcioId, Guid notificacionId, CancellationToken ct = default)
    {
        var n = await _db.Notificaciones
            .Where(x => x.Id == notificacionId && x.ConsorcioId == consorcioId && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result> MarcarTodasLeidasAsync(Guid consorcioId, CancellationToken ct = default)
    {
        await _db.Notificaciones
            .Where(x => x.ConsorcioId == consorcioId && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task CrearAsync(
        Guid consorcioId, TipoNotificacion tipo, string titulo, string cuerpo,
        string? enlace = null, CancellationToken ct = default)
    {
        try
        {
            var adminId = await _db.Consorcios
                .IgnoreQueryFilters()
                .Where(c => c.Id == consorcioId)
                .Select(c => (Guid?)c.AdministradorId)
                .FirstOrDefaultAsync(ct);
            if (adminId is not { } admin) return;

            _db.Notificaciones.Add(new Notificacion
            {
                AdministradorId = admin,
                ConsorcioId = consorcioId,
                Tipo = tipo,
                Titulo = titulo.Trim(),
                Cuerpo = cuerpo.Trim(),
                Enlace = string.IsNullOrWhiteSpace(enlace) ? null : enlace.Trim(),
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo crear la notificación {Tipo} para el consorcio {Consorcio}", tipo, consorcioId);
        }
    }
}
