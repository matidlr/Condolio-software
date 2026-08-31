using Condolio.Application.Common;
using Condolio.Application.Personal;
using Condolio.Domain.Personal;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Personal;

public class PersonalService : IPersonalService, ICredencialCasetaService
{
    private readonly CondolioDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public PersonalService(CondolioDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ================= Staff =================

    public async Task<Result<PersonalListaDto>> ListarAsync(Guid consorcioId, string? busqueda, CancellationToken ct = default)
    {
        var q = (busqueda ?? "").Trim().ToLowerInvariant();
        var miembros = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && !p.EsDispositivo)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        var dispositivos = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && p.EsDispositivo && p.UsuarioId != null)
            .Select(p => new CredRef(p.UsuarioId!, p.Id, p.Nombre))
            .ToListAsync(ct);
        var porUsuario = dispositivos.ToDictionary(d => d.UsuarioId, d => d);

        var filtrados = miembros
            .Where(p => q.Length == 0 || (p.Nombre + " " + p.Apellido).ToLowerInvariant().Contains(q))
            .Select(p => ToDto(p, porUsuario)).ToList();

        return Result<PersonalListaDto>.Ok(new PersonalListaDto(
            filtrados,
            miembros.Count,
            miembros.Count(p => p.Tipo == TipoPersonal.Seguridad),
            miembros.Count(p => p.UsuarioId != null)));
    }

    public async Task<Result<IReadOnlyList<CredencialOpcionDto>>> CredencialesDisponiblesAsync(
        Guid consorcioId, Guid? incluirId = null, CancellationToken ct = default)
    {
        var dispositivos = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && p.EsDispositivo && p.UsuarioId != null && p.Activo)
            .Select(p => new { p.Id, p.Nombre, p.Email, p.UsuarioId })
            .ToListAsync(ct);

        // usuarios ya vinculados a algún miembro del staff (excepto el que estamos editando)
        var vinculados = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && !p.EsDispositivo && p.UsuarioId != null && (incluirId == null || p.Id != incluirId))
            .Select(p => p.UsuarioId!)
            .ToListAsync(ct);
        var usados = vinculados.ToHashSet();

        var lista = dispositivos
            .Where(d => !usados.Contains(d.UsuarioId!))
            .Select(d => new CredencialOpcionDto(d.Id, d.Nombre, d.Email ?? "—"))
            .ToList();
        return Result<IReadOnlyList<CredencialOpcionDto>>.Ok(lista);
    }

    public async Task<Result<PersonalCreadoDto>> CrearAsync(Guid consorcioId, GuardarPersonalDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<PersonalCreadoDto>.Fail("El nombre es obligatorio.");

        var admin = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (admin is not { } adminId) return Result<PersonalCreadoDto>.Fail("Consorcio no encontrado.");

        var m = new MiembroPersonal
        {
            AdministradorId = adminId,
            ConsorcioId = consorcioId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido?.Trim() ?? "",
            Tipo = dto.Tipo,
            EsDispositivo = false,
        };

        var (err, cred) = await VincularAsync(m, consorcioId, dto.CredencialId, ct);
        if (err is not null) return Result<PersonalCreadoDto>.Fail(err);

        _db.Personal.Add(m);
        await _db.SaveChangesAsync(ct);
        return Result<PersonalCreadoDto>.Ok(new PersonalCreadoDto(ToDtoSimple(m, cred), null));
    }

    public async Task<Result<MiembroPersonalDto>> ActualizarAsync(Guid consorcioId, Guid id, GuardarPersonalDto dto, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result<MiembroPersonalDto>.Fail("Personal no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<MiembroPersonalDto>.Fail("El nombre es obligatorio.");

        m.Nombre = dto.Nombre.Trim();
        m.Apellido = dto.Apellido?.Trim() ?? "";
        m.Tipo = dto.Tipo;

        if (!m.EsDispositivo)
        {
            var (err, cred) = await VincularAsync(m, consorcioId, dto.CredencialId, ct);
            if (err is not null) return Result<MiembroPersonalDto>.Fail(err);
            await _db.SaveChangesAsync(ct);
            return Result<MiembroPersonalDto>.Ok(ToDtoSimple(m, cred));
        }

        await _db.SaveChangesAsync(ct);
        return Result<MiembroPersonalDto>.Ok(ToDtoSimple(m, null));
    }

    /// <summary>Vincula/desvincula el miembro a la credencial indicada (copia su UsuarioId/Email).</summary>
    private async Task<(string? error, MiembroPersonal? cred)> VincularAsync(
        MiembroPersonal m, Guid consorcioId, Guid? credencialId, CancellationToken ct)
    {
        if (credencialId is not { } cid)
        {
            m.UsuarioId = null;
            m.Email = null;
            return (null, null);
        }
        var cred = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == cid && x.ConsorcioId == consorcioId && x.EsDispositivo && x.UsuarioId != null, ct);
        if (cred is null) return ("La credencial de caseta no existe.", null);
        m.UsuarioId = cred.UsuarioId;
        m.Email = cred.Email;
        return (null, cred);
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result.Fail("No encontrado.");

        // Solo se borra la cuenta si es la credencial dueña (dispositivo), no un staff vinculado.
        if (m.EsDispositivo && m.UsuarioId is { } uid)
        {
            var user = await _users.FindByIdAsync(uid);
            if (user is not null) await _users.DeleteAsync(user);
            // desvincular staff que usaban esta credencial
            var staff = await _db.Personal.IgnoreQueryFilters()
                .Where(x => x.ConsorcioId == consorcioId && !x.EsDispositivo && x.UsuarioId == uid).ToListAsync(ct);
            foreach (var s in staff) { s.UsuarioId = null; s.Email = null; }
        }
        _db.Personal.Remove(m);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ================= Credenciales de caseta =================

    public async Task<Result<CredencialesCasetaListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var disp = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && p.EsDispositivo)
            .OrderByDescending(p => p.CreadoUtc)
            .ToListAsync(ct);

        var lista = disp.Select(p => new CredencialCasetaDto(p.Id, p.Nombre, p.Email ?? "—", p.Activo, p.CreadoUtc)).ToList();
        var ultimo = disp.FirstOrDefault();

        return Result<CredencialesCasetaListaDto>.Ok(new CredencialesCasetaListaDto(
            lista, disp.Count, disp.Count(p => p.Activo), ultimo?.CreadoUtc, ultimo?.Nombre));
    }

    public async Task<Result<CredencialGeneradaDto>> CrearAsync(Guid consorcioId, string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            return Result<CredencialGeneradaDto>.Fail("Ingresá un nombre para el dispositivo.");

        var datos = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => new { c.AdministradorId, c.Nombre }).FirstOrDefaultAsync(ct);
        if (datos is null) return Result<CredencialGeneradaDto>.Fail("Consorcio no encontrado.");

        var slug = Slug(datos.Nombre);
        var n = await _db.Personal.IgnoreQueryFilters().CountAsync(p => p.ConsorcioId == consorcioId && p.EsDispositivo, ct) + 1;
        string email;
        do { email = $"{slug}-c{n}@caseta.condolio.app"; n++; }
        while (await _users.FindByEmailAsync(email) is not null);

        var (user, generada, error) = await CrearCuentaAsync(datos.AdministradorId, email, ct);
        if (error is not null) return Result<CredencialGeneradaDto>.Fail(error);

        var m = new MiembroPersonal
        {
            AdministradorId = datos.AdministradorId,
            ConsorcioId = consorcioId,
            Nombre = nombre.Trim(),
            Apellido = "",
            Tipo = TipoPersonal.Seguridad,
            EsDispositivo = true,
            UsuarioId = user!.Id,
            Email = email,
        };
        _db.Personal.Add(m);
        await _db.SaveChangesAsync(ct);

        return Result<CredencialGeneradaDto>.Ok(new CredencialGeneradaDto(
            new CredencialCasetaDto(m.Id, m.Nombre, email, true, m.CreadoUtc), email, generada!));
    }

    public async Task<Result<CredencialGeneradaDto>> RegenerarClaveAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId && x.EsDispositivo && x.UsuarioId != null, ct);
        if (m is null) return Result<CredencialGeneradaDto>.Fail("Dispositivo no encontrado.");

        var user = await _users.FindByIdAsync(m.UsuarioId!);
        if (user is null) return Result<CredencialGeneradaDto>.Fail("Cuenta no encontrada.");

        var nueva = GenerarClave();
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var res = await _users.ResetPasswordAsync(user, token, nueva);
        if (!res.Succeeded)
            return Result<CredencialGeneradaDto>.Fail(string.Join(" ", res.Errors.Select(e => e.Description)));

        return Result<CredencialGeneradaDto>.Ok(new CredencialGeneradaDto(
            new CredencialCasetaDto(m.Id, m.Nombre, m.Email ?? "—", m.Activo, m.CreadoUtc), m.Email ?? "—", nueva));
    }

    public async Task<Result> CambiarEstadoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result.Fail("Dispositivo no encontrado.");
        m.Activo = activo;
        if (m.UsuarioId is { } uid)
        {
            var user = await _users.FindByIdAsync(uid);
            if (user is not null)
            {
                await _users.SetLockoutEnabledAsync(user, !activo);
                await _users.SetLockoutEndDateAsync(user, activo ? null : DateTimeOffset.MaxValue);
            }
        }
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    Task<Result> ICredencialCasetaService.EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct) =>
        EliminarAsync(consorcioId, id, ct);

    // ---- helpers ----

    private async Task<(ApplicationUser? user, string? password, string? error)> CrearCuentaAsync(
        Guid adminId, string email, CancellationToken ct)
    {
        if (await _users.FindByEmailAsync(email) is not null)
            return (null, null, "Ya existe una cuenta con ese email.");

        var pass = GenerarClave();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = "Caseta",
            Apellido = "",
            AdministradorId = adminId,
        };
        var res = await _users.CreateAsync(user, pass);
        if (!res.Succeeded)
            return (null, null, string.Join(" ", res.Errors.Select(e => e.Description)));
        await _users.AddToRoleAsync(user, Roles.Personal);
        return (user, pass, null);
    }

    /// <summary>Clave temporal corta y legible que cumple la política de Identity (mayús + minús + dígito, sin caracteres ambiguos).</summary>
    private static string GenerarClave()
    {
        var r = Random.Shared;
        const string lo = "abcdefghjkmnpqrstuvwxyz";
        const string up = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string di = "23456789";
        var chars = new[]
        {
            lo[r.Next(lo.Length)], lo[r.Next(lo.Length)],
            up[r.Next(up.Length)], up[r.Next(up.Length)],
            di[r.Next(di.Length)], di[r.Next(di.Length)],
        };
        return new string(chars.OrderBy(_ => r.Next()).ToArray());
    }

    private static string Slug(string s)
    {
        var limpio = new string(s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        limpio = string.Join("-", limpio.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 ? "caseta" : limpio[..Math.Min(limpio.Length, 20)];
    }

    private sealed record CredRef(string UsuarioId, Guid Id, string Nombre);

    private static MiembroPersonalDto ToDto(MiembroPersonal m, Dictionary<string, CredRef> credPorUsuario)
    {
        Guid? credId = null; string? credNombre = null;
        if (m.UsuarioId is { } uid && credPorUsuario.TryGetValue(uid, out var c))
        {
            credId = c.Id; credNombre = c.Nombre;
        }
        return new(m.Id, m.Nombre, m.Apellido, m.Tipo, m.UsuarioId != null, m.Email, credId, credNombre, m.Activo);
    }

    private static MiembroPersonalDto ToDtoSimple(MiembroPersonal m, MiembroPersonal? cred) => new(
        m.Id, m.Nombre, m.Apellido, m.Tipo, m.UsuarioId != null, m.Email, cred?.Id, cred?.Nombre, m.Activo);
}
