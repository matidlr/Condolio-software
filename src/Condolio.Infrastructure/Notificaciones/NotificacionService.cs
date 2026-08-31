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

    // ================= Administrador =================

    public async Task<Result<NotificacionListaDto>> ListarAsync(
        Guid consorcioId, bool soloNoLeidas = false, TipoNotificacion? tipo = null, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<NotificacionListaDto>.Fail("Consorcio no encontrado.");

        var baseQuery = _db.Notificaciones
            .Where(n => n.ConsorcioId == consorcioId && n.DestinatarioUsuarioId == null);

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
        var q = _db.Notificaciones.Where(n => n.ConsorcioId == consorcioId && n.DestinatarioUsuarioId == null);
        var total = await q.CountAsync(ct);
        var noLeidas = await q.CountAsync(n => n.LeidaUtc == null, ct);
        return Result<NotificacionResumenDto>.Ok(new NotificacionResumenDto(total, noLeidas));
    }

    public async Task<Result> MarcarLeidaAsync(Guid consorcioId, Guid notificacionId, CancellationToken ct = default)
    {
        await _db.Notificaciones
            .Where(x => x.Id == notificacionId && x.ConsorcioId == consorcioId && x.DestinatarioUsuarioId == null && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result> MarcarTodasLeidasAsync(Guid consorcioId, CancellationToken ct = default)
    {
        await _db.Notificaciones
            .Where(x => x.ConsorcioId == consorcioId && x.DestinatarioUsuarioId == null && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task CrearAsync(
        Guid consorcioId, TipoNotificacion tipo, string titulo, string cuerpo,
        string? enlace = null, CancellationToken ct = default)
    {
        try
        {
            var adminId = await _db.Consorcios.IgnoreQueryFilters()
                .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
            if (adminId is not { } admin) return;

            _db.Notificaciones.Add(new Notificacion
            {
                AdministradorId = admin,
                ConsorcioId = consorcioId,
                Tipo = tipo,
                Categoria = CategoriaNotificacion.General,
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

    // ================= Residente =================

    public async Task CrearParaUsuarioAsync(
        Guid consorcioId, string usuarioId, CategoriaNotificacion categoria, TipoNotificacion tipo,
        string titulo, string cuerpo, string? enlace = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(usuarioId)) return;
            var adminId = await _db.Consorcios.IgnoreQueryFilters()
                .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
            if (adminId is not { } admin) return;

            var pref = await _db.PreferenciasNotificacion.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);
            if (pref is not null && !pref.AppHabilitada(categoria)) return;

            _db.Notificaciones.Add(new Notificacion
            {
                AdministradorId = admin,
                ConsorcioId = consorcioId,
                DestinatarioUsuarioId = usuarioId,
                Categoria = categoria,
                Tipo = tipo,
                Titulo = titulo.Trim(),
                Cuerpo = cuerpo.Trim(),
                Enlace = string.IsNullOrWhiteSpace(enlace) ? null : enlace.Trim(),
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo crear la notificación de residente {Tipo}", tipo);
        }
    }

    public async Task CrearParaResidentesAsync(
        Guid consorcioId, CategoriaNotificacion categoria, TipoNotificacion tipo,
        string titulo, string cuerpo, string? enlace = null, string? excluirUsuarioId = null, CancellationToken ct = default)
    {
        try
        {
            var adminId = await _db.Consorcios.IgnoreQueryFilters()
                .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
            if (adminId is not { } admin) return;

            var usuarios = await _db.UnidadPersonas.IgnoreQueryFilters()
                .Where(p => p.Unidad.ConsorcioId == consorcioId && p.UsuarioId != null)
                .Select(p => p.UsuarioId!)
                .Distinct()
                .ToListAsync(ct);

            var prefs = await _db.PreferenciasNotificacion.IgnoreQueryFilters()
                .Where(p => usuarios.Contains(p.UsuarioId))
                .ToDictionaryAsync(p => p.UsuarioId, ct);

            var t = titulo.Trim();
            var c = cuerpo.Trim();
            var e = string.IsNullOrWhiteSpace(enlace) ? null : enlace.Trim();

            foreach (var uid in usuarios)
            {
                if (uid == excluirUsuarioId) continue;
                if (prefs.TryGetValue(uid, out var pref) && !pref.AppHabilitada(categoria)) continue;
                _db.Notificaciones.Add(new Notificacion
                {
                    AdministradorId = admin,
                    ConsorcioId = consorcioId,
                    DestinatarioUsuarioId = uid,
                    Categoria = categoria,
                    Tipo = tipo,
                    Titulo = t,
                    Cuerpo = c,
                    Enlace = e,
                });
            }
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudieron crear notificaciones de residentes {Tipo}", tipo);
        }
    }

    public async Task<Result<NotificacionListaDto>> ListarUsuarioAsync(string usuarioId, bool soloNoLeidas = false, CancellationToken ct = default)
    {
        var baseQuery = _db.Notificaciones.IgnoreQueryFilters().Where(n => n.DestinatarioUsuarioId == usuarioId);
        var total = await baseQuery.CountAsync(ct);
        var noLeidas = await baseQuery.CountAsync(n => n.LeidaUtc == null, ct);

        var q = soloNoLeidas ? baseQuery.Where(n => n.LeidaUtc == null) : baseQuery;
        var lista = await q
            .OrderByDescending(n => n.CreadoUtc)
            .Take(MaxLista)
            .Select(n => new NotificacionDto(n.Id, n.Tipo, n.Titulo, n.Cuerpo, n.Enlace, n.LeidaUtc != null, n.CreadoUtc))
            .ToListAsync(ct);

        return Result<NotificacionListaDto>.Ok(new NotificacionListaDto(lista, total, noLeidas));
    }

    public async Task<Result<NotificacionResumenDto>> ResumenUsuarioAsync(string usuarioId, CancellationToken ct = default)
    {
        var q = _db.Notificaciones.IgnoreQueryFilters().Where(n => n.DestinatarioUsuarioId == usuarioId);
        var total = await q.CountAsync(ct);
        var noLeidas = await q.CountAsync(n => n.LeidaUtc == null, ct);
        return Result<NotificacionResumenDto>.Ok(new NotificacionResumenDto(total, noLeidas));
    }

    public async Task<Result> MarcarLeidaUsuarioAsync(string usuarioId, Guid notificacionId, CancellationToken ct = default)
    {
        await _db.Notificaciones.IgnoreQueryFilters()
            .Where(x => x.Id == notificacionId && x.DestinatarioUsuarioId == usuarioId && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result> MarcarTodasLeidasUsuarioAsync(string usuarioId, CancellationToken ct = default)
    {
        await _db.Notificaciones.IgnoreQueryFilters()
            .Where(x => x.DestinatarioUsuarioId == usuarioId && x.LeidaUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeidaUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result<PreferenciasNotificacionDto>> PreferenciasAsync(string usuarioId, CancellationToken ct = default)
    {
        var p = await _db.PreferenciasNotificacion.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct)
            ?? new PreferenciasNotificacion();
        return Result<PreferenciasNotificacionDto>.Ok(ToDto(p));
    }

    public async Task<Result<PreferenciasNotificacionDto>> GuardarPreferenciasAsync(
        string usuarioId, PreferenciasNotificacionDto dto, CancellationToken ct = default)
    {
        var p = await _db.PreferenciasNotificacion.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, ct);
        if (p is null)
        {
            p = new PreferenciasNotificacion { UsuarioId = usuarioId };
            _db.PreferenciasNotificacion.Add(p);
        }
        p.SeguridadApp = dto.SeguridadApp; p.SeguridadMail = dto.SeguridadMail;
        p.FinanzasApp = dto.FinanzasApp; p.FinanzasMail = dto.FinanzasMail;
        p.ComunidadApp = dto.ComunidadApp; p.ComunidadMail = dto.ComunidadMail;
        p.EventosApp = dto.EventosApp; p.EventosMail = dto.EventosMail;
        p.EdificioApp = dto.EdificioApp; p.EdificioMail = dto.EdificioMail;
        await _db.SaveChangesAsync(ct);
        return Result<PreferenciasNotificacionDto>.Ok(ToDto(p));
    }

    private static PreferenciasNotificacionDto ToDto(PreferenciasNotificacion p) => new(
        p.SeguridadApp, p.SeguridadMail, p.FinanzasApp, p.FinanzasMail,
        p.ComunidadApp, p.ComunidadMail, p.EventosApp, p.EventosMail,
        p.EdificioApp, p.EdificioMail);
}
