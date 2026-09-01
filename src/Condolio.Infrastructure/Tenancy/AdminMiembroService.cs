using Condolio.Application.Common;
using Condolio.Application.Tenancy;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Condolio.Infrastructure.Tenancy;

public class AdminMiembroService : IAdminMiembroService
{
    private const string PrefijoInvitacion = "inv:";

    private readonly CondolioDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailSender _email;
    private readonly string _frontendUrl;

    public AdminMiembroService(CondolioDbContext db, UserManager<ApplicationUser> users, IEmailSender email, IConfiguration config)
    {
        _db = db;
        _users = users;
        _email = email;
        _frontendUrl = (config["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
    }

    public async Task<Result<IReadOnlyList<AdminMiembroDto>>> ListarAsync(Guid administradorId, CancellationToken ct = default)
    {
        await GarantizarDuenoAsync(administradorId, ct);

        var miembros = await _db.AdminMiembros
            .Where(m => m.AdministradorId == administradorId)
            .ToListAsync(ct);

        var ids = miembros.Select(m => m.UsuarioId).ToList();
        var users = await _db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Email })
            .ToDictionaryAsync(u => u.Id, ct);

        var lista = miembros
            .Select(m =>
            {
                users.TryGetValue(m.UsuarioId, out var u);
                return new AdminMiembroDto(
                    m.UsuarioId,
                    u is null ? "—" : $"{u.Nombre} {u.Apellido}".Trim(),
                    u?.Email ?? "—",
                    m.EsGeneral, m.EsDueno, false, ParseAreas(m.AreasCsv));
            })
            .ToList();

        var pendientes = await _db.InvitacionesAdmin
            .Where(i => i.AdministradorId == administradorId && i.Estado == EstadoInvitacionAdmin.Pendiente
                && i.ExpiraUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreadoUtc)
            .ToListAsync(ct);

        lista.AddRange(pendientes.Select(i => new AdminMiembroDto(
            PrefijoInvitacion + i.Id, i.Email, i.Email, i.EsGeneral, false, true, ParseAreas(i.AreasCsv))));

        return Result<IReadOnlyList<AdminMiembroDto>>.Ok(
            lista.OrderByDescending(m => m.EsDueno).ThenBy(m => m.Pendiente).ThenBy(m => m.Nombre).ToList());
    }

    public async Task<Result<AdminMiembroDto>> AgregarAsync(
        Guid administradorId, string invitadoPorUsuarioId, AgregarAdminDto dto, CancellationToken ct = default)
    {
        var email = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<AdminMiembroDto>.Fail("Ingresá un correo válido.");
        if (!dto.EsGeneral && dto.Areas.Count == 0)
            return Result<AdminMiembroDto>.Fail("Elegí al menos un área para el acceso limitado.");

        var user = await _users.FindByEmailAsync(email);

        if (user is null)
        {
            if (await _db.InvitacionesAdmin.AnyAsync(i => i.AdministradorId == administradorId && i.Email == email
                    && i.Estado == EstadoInvitacionAdmin.Pendiente, ct))
                return Result<AdminMiembroDto>.Fail("Ya hay una invitación pendiente para ese correo.");

            var consorcioNombre = await _db.Administradores.Where(a => a.Id == administradorId)
                .Select(a => a.RazonSocial).FirstOrDefaultAsync(ct) ?? "tu cuenta";

            var invitacion = new InvitacionAdmin
            {
                AdministradorId = administradorId,
                Email = email,
                EsGeneral = dto.EsGeneral,
                AreasCsv = dto.EsGeneral ? string.Empty : SerializeAreas(dto.Areas),
                InvitadoPorUsuarioId = invitadoPorUsuarioId,
            };
            _db.InvitacionesAdmin.Add(invitacion);
            await _db.SaveChangesAsync(ct);

            await EnviarInvitacionAsync(email, consorcioNombre, ct);

            return Result<AdminMiembroDto>.Ok(new AdminMiembroDto(
                PrefijoInvitacion + invitacion.Id, email, email, invitacion.EsGeneral, false, true, ParseAreas(invitacion.AreasCsv)));
        }

        if (await _db.AdminMiembros.AnyAsync(m => m.AdministradorId == administradorId && m.UsuarioId == user.Id, ct))
            return Result<AdminMiembroDto>.Fail("Esa persona ya es administradora de esta cuenta.");

        var esDuenoDeOtro = await _db.Administradores.AnyAsync(a => a.UsuarioId == user.Id && a.Id != administradorId, ct);
        if (esDuenoDeOtro)
            return Result<AdminMiembroDto>.Fail("Esa persona ya tiene su propia cuenta de administrador.");

        if (user.AdministradorId is { } actual && actual != administradorId)
            return Result<AdminMiembroDto>.Fail("Esa persona ya pertenece a otra comunidad. Tiene que usar un correo sin cuenta.");

        var miembro = new AdminMiembro
        {
            AdministradorId = administradorId,
            UsuarioId = user.Id,
            EsGeneral = dto.EsGeneral,
            AreasCsv = dto.EsGeneral ? string.Empty : SerializeAreas(dto.Areas),
            EsDueno = false,
        };
        _db.AdminMiembros.Add(miembro);

        user.AdministradorId ??= administradorId;
        await _db.SaveChangesAsync(ct);

        if (!await _users.IsInRoleAsync(user, Roles.Administrador))
            await _users.AddToRoleAsync(user, Roles.Administrador);

        return Result<AdminMiembroDto>.Ok(new AdminMiembroDto(
            user.Id, user.NombreCompleto, user.Email ?? email, miembro.EsGeneral, false, false, ParseAreas(miembro.AreasCsv)));
    }

    public async Task<Result<AdminMiembroDto>> CambiarRolAsync(
        Guid administradorId, string usuarioId, GuardarRolAdminDto dto, CancellationToken ct = default)
    {
        await GarantizarDuenoAsync(administradorId, ct);
        var miembro = await _db.AdminMiembros
            .FirstOrDefaultAsync(m => m.AdministradorId == administradorId && m.UsuarioId == usuarioId, ct);
        if (miembro is null) return Result<AdminMiembroDto>.Fail("Administrador no encontrado.");

        if (!dto.EsGeneral)
        {
            if (dto.Areas.Count == 0) return Result<AdminMiembroDto>.Fail("Elegí al menos un área.");
            if (miembro.EsDueno)
                return Result<AdminMiembroDto>.Fail("El dueño de la cuenta no puede tener acceso limitado.");
            var generales = await _db.AdminMiembros
                .CountAsync(m => m.AdministradorId == administradorId && m.EsGeneral, ct);
            if (miembro.EsGeneral && generales <= 1)
                return Result<AdminMiembroDto>.Fail("Tiene que quedar al menos un administrador general. Nombrá a otro primero.");
        }

        miembro.EsGeneral = dto.EsGeneral;
        miembro.AreasCsv = dto.EsGeneral ? string.Empty : SerializeAreas(dto.Areas);
        await _db.SaveChangesAsync(ct);

        var u = await _db.Users.Where(x => x.Id == usuarioId)
            .Select(x => new { x.Nombre, x.Apellido, x.Email }).FirstOrDefaultAsync(ct);
        return Result<AdminMiembroDto>.Ok(new AdminMiembroDto(
            usuarioId, u is null ? "—" : $"{u.Nombre} {u.Apellido}".Trim(), u?.Email ?? "—",
            miembro.EsGeneral, miembro.EsDueno, false, ParseAreas(miembro.AreasCsv)));
    }

    public async Task<Result> QuitarAsync(Guid administradorId, string id, CancellationToken ct = default)
    {
        if (id.StartsWith(PrefijoInvitacion, StringComparison.Ordinal))
        {
            if (!Guid.TryParse(id[PrefijoInvitacion.Length..], out var invId))
                return Result.Fail("Invitación no encontrada.");
            var invitacion = await _db.InvitacionesAdmin
                .FirstOrDefaultAsync(i => i.Id == invId && i.AdministradorId == administradorId, ct);
            if (invitacion is null) return Result.Fail("Invitación no encontrada.");
            invitacion.Estado = EstadoInvitacionAdmin.Cancelada;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }

        var usuarioId = id;
        var miembro = await _db.AdminMiembros
            .FirstOrDefaultAsync(m => m.AdministradorId == administradorId && m.UsuarioId == usuarioId, ct);
        if (miembro is null) return Result.Fail("Administrador no encontrado.");
        if (miembro.EsDueno) return Result.Fail("No podés quitar al dueño de la cuenta.");
        if (miembro.EsGeneral)
        {
            var generales = await _db.AdminMiembros
                .CountAsync(m => m.AdministradorId == administradorId && m.EsGeneral, ct);
            if (generales <= 1) return Result.Fail("Tiene que quedar al menos un administrador general.");
        }

        _db.AdminMiembros.Remove(miembro);
        await _db.SaveChangesAsync(ct);

        // Le sacamos el rol Administrador solo si no es admin en otro lado.
        var user = await _users.FindByIdAsync(usuarioId);
        if (user is not null)
        {
            var otras = await _db.AdminMiembros.AnyAsync(m => m.UsuarioId == usuarioId && m.AdministradorId != administradorId, ct);
            var esDuenoPropio = await _db.Administradores.AnyAsync(a => a.UsuarioId == usuarioId, ct);
            var sigueVinculado = await _db.UnidadPersonas.IgnoreQueryFilters().AnyAsync(p => p.UsuarioId == usuarioId, ct)
                || await _db.Personal.IgnoreQueryFilters().AnyAsync(p => p.UsuarioId == usuarioId, ct);

            if (!otras && !esDuenoPropio && await _users.IsInRoleAsync(user, Roles.Administrador))
                await _users.RemoveFromRoleAsync(user, Roles.Administrador);

            if (!otras && !esDuenoPropio && !sigueVinculado)
            {
                user.AdministradorId = null;
                await _users.UpdateAsync(user);
            }
        }
        return Result.Ok();
    }

    /// <summary>Si el dueño del tenant no tiene fila en AdminMiembros, la crea.</summary>
    private async Task GarantizarDuenoAsync(Guid administradorId, CancellationToken ct)
    {
        var duenoId = await _db.Administradores.Where(a => a.Id == administradorId)
            .Select(a => a.UsuarioId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(duenoId)) return;

        if (!await _db.AdminMiembros.AnyAsync(m => m.AdministradorId == administradorId && m.UsuarioId == duenoId, ct))
        {
            _db.AdminMiembros.Add(new AdminMiembro
            {
                AdministradorId = administradorId,
                UsuarioId = duenoId,
                EsGeneral = true,
                EsDueno = true,
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private Task EnviarInvitacionAsync(string email, string consorcioNombre, CancellationToken ct)
    {
        var sitio = _frontendUrl.Replace("https://", "").Replace("http://", "").TrimEnd('/');
        var cuerpo = $"""
            <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;max-width:520px;margin:0 auto;color:#1f2937">
              <p style="font-size:13px;color:#6b7280;text-transform:uppercase;letter-spacing:.04em;margin:0 0 4px">Invitación de administrador</p>
              <h1 style="font-size:22px;margin:0 0 16px">Te invitaron a administrar {consorcioNombre}</h1>
              <p style="line-height:1.55">Te sumaron como administrador de <b>{consorcioNombre}</b> en Condolio.</p>
              <h2 style="font-size:16px;margin:24px 0 8px">Cómo entrar</h2>
              <ol style="line-height:1.7;padding-left:20px">
                <li>Ingresá al sitio <a href="{_frontendUrl}" style="color:#2563eb">{sitio}</a></li>
                <li>Registrate con el correo <b>{email}</b> — es el que usó quien te invitó.</li>
                <li>Listo. Vas a entrar directamente como administrador de <b>{consorcioNombre}</b>.</li>
              </ol>
              <p style="font-size:13px;color:#6b7280">— El equipo de Condolio</p>
            </div>
            """;
        return _email.EnviarAsync(email, $"Te invitaron a administrar {consorcioNombre}", cuerpo, ct);
    }

    private static IReadOnlyList<AreaAdmin> ParseAreas(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<AreaAdmin>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Enum.TryParse<AreaAdmin>(s, out _))
                .Select(Enum.Parse<AreaAdmin>)
                .Distinct()
                .ToArray();

    private static string SerializeAreas(IReadOnlyList<AreaAdmin>? areas) =>
        areas is null || areas.Count == 0 ? string.Empty : string.Join(',', areas.Distinct());
}
