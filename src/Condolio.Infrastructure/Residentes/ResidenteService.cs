using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Residentes;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Condolio.Infrastructure.Residentes;

public class ResidenteService : IResidenteService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IEmailSender _email;
    private readonly UserManager<ApplicationUser> _users;
    private readonly string _frontendUrl;

    public ResidenteService(CondolioDbContext db, ITenantContext tenant, IEmailSender email,
        UserManager<ApplicationUser> users, IConfiguration config)
    {
        _db = db;
        _tenant = tenant;
        _email = email;
        _users = users;
        _frontendUrl = (config["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
    }

    public async Task<Result<DirectorioDto>> DirectorioAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<DirectorioDto>.Fail("Consorcio no encontrado.");

        var residentes = await _db.UnidadPersonas
            .Where(p => p.Unidad.ConsorcioId == consorcioId)
            .OrderBy(p => p.Unidad.Piso).ThenBy(p => p.Unidad.Nombre).ThenBy(p => p.Rol)
            .Select(p => new ResidenteDto(
                p.Id, p.Nombre, p.Apellido, p.Email, p.Telefono, p.Rol,
                p.EsContactoPrincipal, p.UsuarioId != null,
                p.UnidadId, p.Unidad.Nombre))
            .ToListAsync(ct);

        var totalUnidades = await _db.Unidades.CountAsync(u => u.ConsorcioId == consorcioId, ct);
        var unidadesConPropietario = await _db.Unidades
            .CountAsync(u => u.ConsorcioId == consorcioId
                && u.Personas.Any(p => p.Rol == RolUnidad.Propietario), ct);

        return Result<DirectorioDto>.Ok(new DirectorioDto(
            residentes,
            residentes.Count,
            residentes.Count(r => r.Rol == RolUnidad.Propietario),
            residentes.Count(r => r.Rol == RolUnidad.Inquilino),
            residentes.Count(r => r.Rol == RolUnidad.Gestor),
            totalUnidades - unidadesConPropietario));
    }

    public async Task<Result<PersonaDetalleDto>> PersonaDetalleAsync(Guid consorcioId, Guid personaId, CancellationToken ct = default)
    {
        var base_ = await _db.UnidadPersonas
            .FirstOrDefaultAsync(p => p.Id == personaId && p.Unidad.ConsorcioId == consorcioId, ct);
        if (base_ is null) return Result<PersonaDetalleDto>.Fail("Residente no encontrado.");

        var email = base_.Email;
        // Todas las asignaciones de esta persona (por email) en el consorcio.
        var personas = string.IsNullOrWhiteSpace(email)
            ? new List<Domain.Unidades.UnidadPersona> { base_ }
            : await _db.UnidadPersonas
                .Include(p => p.Unidad)
                .Where(p => p.Unidad.ConsorcioId == consorcioId && p.Email == email)
                .ToListAsync(ct);

        var unidades = personas
            .OrderBy(p => p.Unidad.Piso).ThenBy(p => p.Unidad.Nombre)
            .Select(p => new PersonaUnidadRefDto(p.Id, p.UnidadId, p.Unidad.Nombre, p.Rol, p.EsContactoPrincipal))
            .ToList();

        var user = string.IsNullOrWhiteSpace(email) ? null
            : await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var roles = user is null ? Array.Empty<string>() : (await _users.GetRolesAsync(user)).ToArray();

        return Result<PersonaDetalleDto>.Ok(new PersonaDetalleDto(
            base_.Nombre, base_.Apellido, email ?? "",
            personas.Select(p => p.Telefono).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
            unidades, roles,
            user is not null,
            user?.EmailConfirmed ?? false,
            !(user?.LockoutEnd > DateTimeOffset.UtcNow),
            personas.Min(p => p.CreadoUtc)));
    }

    public async Task<Result> ActualizarContactoAsync(Guid consorcioId, Guid personaId, ActualizarPersonaContactoDto dto, CancellationToken ct = default)
    {
        var persona = await _db.UnidadPersonas
            .FirstOrDefaultAsync(p => p.Id == personaId && p.Unidad.ConsorcioId == consorcioId, ct);
        if (persona is null) return Result.Fail("Residente no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result.Fail("El nombre es obligatorio.");

        var email = persona.Email;
        var tel = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();

        // Aplica a todas las asignaciones de la misma persona.
        var todas = string.IsNullOrWhiteSpace(email)
            ? new List<Domain.Unidades.UnidadPersona> { persona }
            : await _db.UnidadPersonas.Where(p => p.Unidad.ConsorcioId == consorcioId && p.Email == email).ToListAsync(ct);
        foreach (var p in todas)
        {
            p.Nombre = dto.Nombre.Trim();
            p.Apellido = dto.Apellido.Trim();
            p.Telefono = tel;
        }
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> RemoverDeComunidadAsync(Guid consorcioId, Guid personaId, CancellationToken ct = default)
    {
        var persona = await _db.UnidadPersonas
            .FirstOrDefaultAsync(p => p.Id == personaId && p.Unidad.ConsorcioId == consorcioId, ct);
        if (persona is null) return Result.Fail("Residente no encontrado.");

        var email = persona.Email;
        var todas = string.IsNullOrWhiteSpace(email)
            ? new List<Domain.Unidades.UnidadPersona> { persona }
            : await _db.UnidadPersonas.Where(p => p.Unidad.ConsorcioId == consorcioId && p.Email == email).ToListAsync(ct);
        _db.UnidadPersonas.RemoveRange(todas);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<InvitacionDto>>> InvitacionesAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<IReadOnlyList<InvitacionDto>>.Fail("Consorcio no encontrado.");

        var raw = await (
            from i in _db.Invitaciones.Where(i => i.ConsorcioId == consorcioId)
            join u in _db.Unidades on i.UnidadId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby i.CreadoUtc descending
            select new { i, unidadNombre = u != null ? u.Nombre : null })
            .ToListAsync(ct);

        var lista = raw.Select(r => Mapear(r.i, r.unidadNombre)).ToList();
        return Result<IReadOnlyList<InvitacionDto>>.Ok(lista);
    }

    public async Task<Result<InvitacionDto>> InvitarAsync(Guid consorcioId, CrearInvitacionDto dto, CancellationToken ct = default)
    {
        var consorcio = await _db.Consorcios.FirstOrDefaultAsync(c => c.Id == consorcioId, ct);
        if (consorcio is null) return Result<InvitacionDto>.Fail("Consorcio no encontrado.");

        var email = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<InvitacionDto>.Fail("Ingresá un correo válido.");

        if (await _db.Invitaciones.AnyAsync(i => i.ConsorcioId == consorcioId
                && i.Email == email && i.Estado == EstadoInvitacion.Pendiente, ct))
            return Result<InvitacionDto>.Fail("Ya hay una invitación pendiente para ese correo.");

        if (dto.UnidadId is { } uid && !await _db.Unidades.AnyAsync(u => u.Id == uid && u.ConsorcioId == consorcioId, ct))
            return Result<InvitacionDto>.Fail("La unidad no pertenece al consorcio.");

        var invitacion = new Invitacion
        {
            ConsorcioId = consorcioId,
            Email = email,
            Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim(),
            UnidadId = dto.UnidadId,
            Rol = dto.Rol,
            InvitadoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
        };
        _db.Invitaciones.Add(invitacion);
        await _db.SaveChangesAsync(ct);

        await EnviarEmail(invitacion, consorcio.Nombre, ct);

        string? unidadNombre = dto.UnidadId is { } id2
            ? await _db.Unidades.Where(u => u.Id == id2).Select(u => u.Nombre).FirstOrDefaultAsync(ct)
            : null;
        return Result<InvitacionDto>.Ok(Mapear(invitacion, unidadNombre));
    }

    public async Task<Result<InvitacionDto>> EditarInvitacionAsync(Guid consorcioId, Guid invitacionId, CrearInvitacionDto dto, CancellationToken ct = default)
    {
        var inv = await _db.Invitaciones.FirstOrDefaultAsync(
            i => i.Id == invitacionId && i.ConsorcioId == consorcioId, ct);
        if (inv is null) return Result<InvitacionDto>.Fail("Invitación no encontrada.");
        if (inv.Estado != EstadoInvitacion.Pendiente)
            return Result<InvitacionDto>.Fail("Solo se pueden editar invitaciones pendientes.");

        var email = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<InvitacionDto>.Fail("Ingresá un correo válido.");
        if (dto.UnidadId is { } uid && !await _db.Unidades.AnyAsync(u => u.Id == uid && u.ConsorcioId == consorcioId, ct))
            return Result<InvitacionDto>.Fail("La unidad no pertenece al consorcio.");

        inv.Email = email;
        inv.Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim();
        inv.UnidadId = dto.UnidadId;
        inv.Rol = dto.Rol;
        await _db.SaveChangesAsync(ct);

        string? un = dto.UnidadId is { } id2
            ? await _db.Unidades.Where(u => u.Id == id2).Select(u => u.Nombre).FirstOrDefaultAsync(ct)
            : null;
        return Result<InvitacionDto>.Ok(Mapear(inv, un));
    }

    public async Task<Result<InvitarLoteResultado>> InvitarLoteAsync(Guid consorcioId, IReadOnlyList<InvitarLoteItem> items, bool notificar, CancellationToken ct = default)
    {
        var consorcio = await _db.Consorcios.FirstOrDefaultAsync(c => c.Id == consorcioId, ct);
        if (consorcio is null) return Result<InvitarLoteResultado>.Fail("Consorcio no encontrado.");

        var unidades = await _db.Unidades
            .Where(u => u.ConsorcioId == consorcioId)
            .ToDictionaryAsync(u => u.Nombre, u => u.Id, StringComparer.OrdinalIgnoreCase, ct);

        var pendientes = (await _db.Invitaciones
            .Where(i => i.ConsorcioId == consorcioId && i.Estado == EstadoInvitacion.Pendiente)
            .Select(i => i.Email)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filas = new List<InvitarLoteResultadoFila>();
        var nuevas = new List<Invitacion>();

        foreach (var item in items)
        {
            var email = item.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            { filas.Add(new(item.Email ?? "", false, "Correo inválido.", null)); continue; }
            if (!pendientes.Add(email))
            { filas.Add(new(email, false, "Ya hay una invitación pendiente.", null)); continue; }

            Guid? unidadId = null;
            string? unidadNombre = null;
            if (!string.IsNullOrWhiteSpace(item.Unidad))
            {
                if (unidades.TryGetValue(item.Unidad.Trim(), out var uid))
                { unidadId = uid; unidadNombre = item.Unidad.Trim(); }
                else
                { filas.Add(new(email, false, $"La unidad \"{item.Unidad}\" no existe.", null)); continue; }
            }

            nuevas.Add(new Invitacion
            {
                ConsorcioId = consorcioId,
                Email = email,
                Nombre = string.IsNullOrWhiteSpace(item.Nombre) ? null : item.Nombre.Trim(),
                UnidadId = unidadId,
                Rol = MapRol(item.Rol),
                InvitadoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            });
            filas.Add(new(email, true, null, unidadNombre));
        }

        if (nuevas.Count > 0)
        {
            _db.Invitaciones.AddRange(nuevas);
            await _db.SaveChangesAsync(ct);
            if (notificar)
                foreach (var inv in nuevas)
                    await EnviarEmail(inv, consorcio.Nombre, ct);
        }

        return Result<InvitarLoteResultado>.Ok(new InvitarLoteResultado(
            filas.Count(f => f.Ok), filas.Count(f => !f.Ok), filas));
    }

    private static RolUnidad MapRol(string? rol) => (rol ?? "").Trim().ToLowerInvariant() switch
    {
        "tenant" or "inquilino" => RolUnidad.Inquilino,
        "admin" or "gestor" or "administrador" => RolUnidad.Gestor,
        _ => RolUnidad.Propietario,
    };

    public async Task<Result> CancelarInvitacionAsync(Guid consorcioId, Guid invitacionId, CancellationToken ct = default)
    {
        var inv = await _db.Invitaciones.FirstOrDefaultAsync(
            i => i.Id == invitacionId && i.ConsorcioId == consorcioId, ct);
        if (inv is null) return Result.Fail("Invitación no encontrada.");
        if (inv.Estado != EstadoInvitacion.Pendiente) return Result.Fail("La invitación ya no está pendiente.");

        inv.Estado = EstadoInvitacion.Cancelada;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<int>> ReenviarPendientesAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var consorcio = await _db.Consorcios.FirstOrDefaultAsync(c => c.Id == consorcioId, ct);
        if (consorcio is null) return Result<int>.Fail("Consorcio no encontrado.");

        var pendientes = await _db.Invitaciones
            .Where(i => i.ConsorcioId == consorcioId && i.Estado == EstadoInvitacion.Pendiente
                && i.Email != "")
            .ToListAsync(ct);

        foreach (var inv in pendientes)
        {
            inv.ExpiraUtc = DateTime.UtcNow.AddDays(14);
            await EnviarEmail(inv, consorcio.Nombre, ct);
        }
        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(pendientes.Count);
    }

    public async Task<Result<IReadOnlyList<ResidenteSinUnidadDto>>> PorAsignarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var adminId = await _db.Consorcios
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (adminId is null) return Result<IReadOnlyList<ResidenteSinUnidadDto>>.Fail("Consorcio no encontrado.");

        var rolResidente = await _db.Roles.Where(r => r.Name == "Residente")
            .Select(r => r.Id).FirstOrDefaultAsync(ct);

        // Usuarios con rol Residente del tenant que no tienen ninguna UnidadPersona.
        var lista = await (
            from u in _db.Users
            where u.AdministradorId == adminId
                && _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == rolResidente)
                && !_db.UnidadPersonas.Any(p => p.UsuarioId == u.Id)
            orderby u.Apellido, u.Nombre
            select new ResidenteSinUnidadDto(u.Id, u.Nombre, u.Apellido, u.Email!))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ResidenteSinUnidadDto>>.Ok(lista);
    }

    public async Task<Result> ReenviarInvitacionAsync(Guid consorcioId, Guid invitacionId, CancellationToken ct = default)
    {
        var inv = await _db.Invitaciones
            .Include(i => i.Consorcio)
            .FirstOrDefaultAsync(i => i.Id == invitacionId && i.ConsorcioId == consorcioId, ct);
        if (inv is null) return Result.Fail("Invitación no encontrada.");
        if (inv.Estado != EstadoInvitacion.Pendiente) return Result.Fail("La invitación ya no está pendiente.");

        inv.ExpiraUtc = DateTime.UtcNow.AddDays(14);
        await _db.SaveChangesAsync(ct);
        await EnviarEmail(inv, inv.Consorcio.Nombre, ct);
        return Result.Ok();
    }

    private Task EnviarEmail(Invitacion inv, string consorcioNombre, CancellationToken ct)
    {
        var sitio = _frontendUrl.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        var saludo = inv.Nombre is null ? "Hola" : $"Hola {inv.Nombre}";
        var cuerpo = $"""
            <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;max-width:520px;margin:0 auto;color:#1f2937">
              <p style="font-size:13px;color:#6b7280;text-transform:uppercase;letter-spacing:.04em;margin:0 0 4px">Invitación de tu comunidad</p>
              <h1 style="font-size:22px;margin:0 0 16px">Tu acceso a la app ya está listo</h1>
              <p style="line-height:1.55">{saludo}, la administración de <b>{consorcioNombre}</b> te invita a Condolio — la app
                donde vas a ver tus cuotas y pagos, los comunicados de la comunidad y las reservas de amenidades.</p>
              <h2 style="font-size:16px;margin:24px 0 8px">Cómo entrar</h2>
              <ol style="line-height:1.7;padding-left:20px">
                <li>Ingresá al sitio <a href="{_frontendUrl}" style="color:#2563eb">{sitio}</a></li>
                <li>Creá tu cuenta con el correo <b>{inv.Email}</b> — es el que registró la administración.</li>
                <li>Listo. Vas a quedar conectado a <b>{consorcioNombre}</b> automáticamente.</li>
              </ol>
              <p style="font-size:13px;color:#6b7280;margin-top:24px">Esta invitación vence el {inv.ExpiraUtc:dd/MM/yyyy}.</p>
              <p style="font-size:13px;color:#6b7280">— El equipo de Condolio</p>
            </div>
            """;
        return _email.EnviarAsync(inv.Email, $"Invitación de {consorcioNombre}", cuerpo, ct);
    }

    private static InvitacionDto Mapear(Invitacion i, string? unidadNombre) => new(
        i.Id, i.Email, i.Nombre, i.Estado.ToString(), i.UnidadId, unidadNombre, i.Rol, i.CreadoUtc, i.ExpiraUtc);
}
