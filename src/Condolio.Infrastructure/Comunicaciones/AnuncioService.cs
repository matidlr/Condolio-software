using Condolio.Application.Comunicaciones;
using Condolio.Application.Common;
using Condolio.Application.Notificaciones;
using Condolio.Domain.Comunicaciones;
using Condolio.Domain.Notificaciones;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Comunicaciones;

public class AnuncioService : IAnuncioService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly INotificacionService _notificaciones;
    private readonly IEmailSender _email;

    public AnuncioService(CondolioDbContext db, ITenantContext tenant, INotificacionService notificaciones, IEmailSender email)
    {
        _db = db;
        _tenant = tenant;
        _notificaciones = notificaciones;
        _email = email;
    }

    public async Task<Result<AnuncioListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<AnuncioListaDto>.Fail("Consorcio no encontrado.");

        var yo = _tenant.UsuarioId ?? string.Empty;
        var anuncios = await _db.Anuncios
            .Where(a => a.ConsorcioId == consorcioId)
            .OrderByDescending(a => a.Fijado)
            .ThenByDescending(a => a.PublicadoUtc)
            .Select(a => new
            {
                a,
                likes = a.Likes.Count,
                comentarios = a.Comentarios.Count,
                yoLike = a.Likes.Any(l => l.UsuarioId == yo),
            })
            .ToListAsync(ct);

        var lista = anuncios.Select(x => Mapear(x.a, x.likes, x.comentarios, x.yoLike)).ToList();
        var dto = new AnuncioListaDto(
            lista, anuncios.Count,
            anuncios.Count(x => x.a.Categoria == CategoriaAnuncio.General),
            anuncios.Count(x => x.a.Categoria == CategoriaAnuncio.Mantenimiento),
            anuncios.Count(x => x.a.Categoria == CategoriaAnuncio.Urgente),
            anuncios.Count(x => x.a.Categoria == CategoriaAnuncio.Evento));
        return Result<AnuncioListaDto>.Ok(dto);
    }

    public async Task<Result<AnuncioDetalleDto>> ObtenerAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default)
    {
        var a = await _db.Anuncios
            .Include(x => x.Comentarios)
            .Include(x => x.Likes)
            .FirstOrDefaultAsync(x => x.Id == anuncioId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result<AnuncioDetalleDto>.Fail("Anuncio no encontrado.");

        var yo = _tenant.UsuarioId ?? string.Empty;
        var dto = Mapear(a, a.Likes.Count, a.Comentarios.Count, a.Likes.Any(l => l.UsuarioId == yo));
        var comentarios = a.Comentarios.OrderBy(c => c.CreadoUtc)
            .Select(c => new AnuncioComentarioDto(c.Id, c.Texto,
                string.IsNullOrWhiteSpace(c.AutorNombre) ? "—" : c.AutorNombre, c.CreadoUtc,
                !string.IsNullOrEmpty(yo) && c.AutorUsuarioId == yo))
            .ToList();
        var likes = a.Likes.OrderBy(l => l.CreadoUtc).Select(l => new AnuncioLikeDto(l.UsuarioNombre)).ToList();
        return Result<AnuncioDetalleDto>.Ok(new AnuncioDetalleDto(dto, comentarios, likes));
    }

    public async Task<Result<AnuncioDto>> CrearAsync(Guid consorcioId, GuardarAnuncioDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<AnuncioDto>.Fail("Consorcio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Cuerpo)) return Result<AnuncioDto>.Fail("El contenido es obligatorio.");

        var autor = await NombreUsuario(_tenant.UsuarioId, ct);
        var a = new Anuncio
        {
            ConsorcioId = consorcioId,
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            AutorNombre = autor ?? "Administración",
        };
        Aplicar(a, dto);
        _db.Anuncios.Add(a);
        await _db.SaveChangesAsync(ct);

        await _notificaciones.CrearParaResidentesAsync(consorcioId, CategoriaNotificacion.Comunidad,
            TipoNotificacion.NuevoAnuncio,
            string.IsNullOrWhiteSpace(a.Titulo) ? "Nuevo anuncio de la administración" : a.Titulo,
            a.Cuerpo.Length > 140 ? a.Cuerpo[..140] + "…" : a.Cuerpo,
            "/portal/muro", excluirUsuarioId: _tenant.UsuarioId, ct: ct);

        await NotificarPorCorreoSiCorrespondeAsync(consorcioId, a, ct);

        return Result<AnuncioDto>.Ok(Mapear(a, 0, 0, false));
    }

    /// <summary>Si la preferencia del consorcio lo pide y el autor es de la administración, manda el anuncio por mail.</summary>
    private async Task NotificarPorCorreoSiCorrespondeAsync(Guid consorcioId, Anuncio a, CancellationToken ct)
    {
        try
        {
            var siempreMail = await _db.PreferenciasConsorcio.IgnoreQueryFilters()
                .Where(p => p.ConsorcioId == consorcioId).Select(p => (bool?)p.AnunciosSiemprePorCorreo).FirstOrDefaultAsync(ct);
            if (siempreMail != true) return;

            // Los anuncios publicados por un residente no se blastean por mail.
            var autorEsResidente = !string.IsNullOrEmpty(a.AutorUsuarioId)
                && await _db.UnidadPersonas.IgnoreQueryFilters()
                    .AnyAsync(p => p.Unidad.ConsorcioId == consorcioId && p.UsuarioId == a.AutorUsuarioId, ct);
            if (autorEsResidente) return;

            var consorcioNombre = await _db.Consorcios.IgnoreQueryFilters()
                .Where(c => c.Id == consorcioId).Select(c => c.Nombre).FirstOrDefaultAsync(ct) ?? "tu comunidad";

            var destinos = await _db.UnidadPersonas.IgnoreQueryFilters()
                .Where(p => p.Unidad.ConsorcioId == consorcioId && p.Email != null && p.Email != "")
                .Select(p => p.Email!)
                .Distinct()
                .ToListAsync(ct);

            var titulo = string.IsNullOrWhiteSpace(a.Titulo) ? "Nuevo anuncio de la administración" : a.Titulo;
            var cuerpoHtml = $"""
                <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;max-width:520px;margin:0 auto;color:#1f2937">
                  <p style="font-size:13px;color:#6b7280;text-transform:uppercase;letter-spacing:.04em;margin:0 0 4px">{consorcioNombre}</p>
                  <h1 style="font-size:20px;margin:0 0 12px">{titulo}</h1>
                  <p style="white-space:pre-wrap;line-height:1.55">{System.Net.WebUtility.HtmlEncode(a.Cuerpo)}</p>
                  <p style="font-size:13px;color:#6b7280;margin-top:20px">— {a.AutorNombre}</p>
                </div>
                """;

            foreach (var email in destinos)
                await _email.EnviarAsync(email, $"{consorcioNombre}: {titulo}", cuerpoHtml, ct);
        }
        catch
        {
            // El envío de mail no debe romper la creación del anuncio.
        }
    }

    public async Task<Result<AnuncioDto>> ActualizarAsync(Guid consorcioId, Guid anuncioId, GuardarAnuncioDto dto, CancellationToken ct = default)
    {
        var a = await _db.Anuncios.FirstOrDefaultAsync(x => x.Id == anuncioId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result<AnuncioDto>.Fail("Anuncio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Cuerpo)) return Result<AnuncioDto>.Fail("El contenido es obligatorio.");

        Aplicar(a, dto);
        await _db.SaveChangesAsync(ct);
        return Result<AnuncioDto>.Ok(Mapear(a, 0, 0, false));
    }

    public async Task<Result> FijarAsync(Guid consorcioId, Guid anuncioId, bool fijar, CancellationToken ct = default)
    {
        var n = await _db.Anuncios.Where(x => x.Id == anuncioId && x.ConsorcioId == consorcioId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Fijado, fijar), ct);
        return n == 0 ? Result.Fail("Anuncio no encontrado.") : Result.Ok();
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default)
    {
        var a = await _db.Anuncios.Include(x => x.Comentarios).Include(x => x.Likes)
            .FirstOrDefaultAsync(x => x.Id == anuncioId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result.Fail("Anuncio no encontrado.");
        _db.AnuncioComentarios.RemoveRange(a.Comentarios);
        _db.AnuncioLikes.RemoveRange(a.Likes);
        _db.Anuncios.Remove(a);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ComentarAsync(Guid consorcioId, Guid anuncioId, string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto)) return Result.Fail("El comentario no puede estar vacío.");
        var a = await _db.Anuncios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == anuncioId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result.Fail("Anuncio no encontrado.");

        var autor = await NombreUsuario(_tenant.UsuarioId, ct) ?? "—";
        _db.AnuncioComentarios.Add(new AnuncioComentario
        {
            AnuncioId = anuncioId,
            AdministradorId = a.AdministradorId,
            Texto = texto.Trim(),
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            AutorNombre = autor,
        });
        await _db.SaveChangesAsync(ct);

        await _notificaciones.CrearAsync(a.ConsorcioId, TipoNotificacion.ComentarioPublicacion,
            "Nuevo comentario en publicación",
            $"{autor} comentó en “{a.Titulo}”.",
            "/panel/anuncios", ct);
        return Result.Ok();
    }

    public async Task<Result> EditarComentarioAsync(Guid consorcioId, Guid anuncioId, Guid comentarioId, string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto)) return Result.Fail("El comentario no puede estar vacío.");
        var c = await _db.AnuncioComentarios
            .FirstOrDefaultAsync(x => x.Id == comentarioId && x.AnuncioId == anuncioId, ct);
        if (c is null) return Result.Fail("Comentario no encontrado.");
        if (!string.IsNullOrEmpty(_tenant.UsuarioId) && c.AutorUsuarioId != _tenant.UsuarioId)
            return Result.Fail("Solo el autor puede editar el comentario.");

        c.Texto = texto.Trim();
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> EliminarComentarioAsync(Guid consorcioId, Guid anuncioId, Guid comentarioId, CancellationToken ct = default)
    {
        var c = await _db.AnuncioComentarios
            .FirstOrDefaultAsync(x => x.Id == comentarioId && x.AnuncioId == anuncioId, ct);
        if (c is null) return Result.Fail("Comentario no encontrado.");

        _db.AnuncioComentarios.Remove(c);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<LikeResultadoDto>> ToggleLikeAsync(Guid consorcioId, Guid anuncioId, CancellationToken ct = default)
    {
        var a = await _db.Anuncios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == anuncioId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result<LikeResultadoDto>.Fail("Anuncio no encontrado.");

        var yo = _tenant.UsuarioId ?? string.Empty;
        var existente = await _db.AnuncioLikes
            .FirstOrDefaultAsync(l => l.AnuncioId == anuncioId && l.UsuarioId == yo, ct);

        if (existente is not null)
        {
            _db.AnuncioLikes.Remove(existente);
        }
        else
        {
            _db.AnuncioLikes.Add(new AnuncioLike
            {
                AnuncioId = anuncioId,
                AdministradorId = a.AdministradorId,
                UsuarioId = yo,
                UsuarioNombre = await NombreUsuario(yo, ct) ?? "—",
            });
        }
        await _db.SaveChangesAsync(ct);

        var likes = await _db.AnuncioLikes.Where(l => l.AnuncioId == anuncioId)
            .OrderBy(l => l.CreadoUtc)
            .Select(l => new AnuncioLikeDto(l.UsuarioNombre)).ToListAsync(ct);
        return Result<LikeResultadoDto>.Ok(new LikeResultadoDto(likes.Count, existente is null, likes));
    }

    // ---- helpers ----

    private async Task<string?> NombreUsuario(string? id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return await _db.Users.Where(u => u.Id == id)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct);
    }

    private static void Aplicar(Anuncio a, GuardarAnuncioDto dto)
    {
        var cuerpo = dto.Cuerpo.Trim();
        a.Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? PrimeraLinea(cuerpo) : dto.Titulo.Trim();
        a.Cuerpo = cuerpo;
        a.Categoria = dto.Categoria;
        a.Fijado = dto.Fijado;
        a.PublicadoUtc = dto.PublicadoUtc ?? a.PublicadoUtc;
        a.EventoFechaUtc = dto.Categoria == CategoriaAnuncio.Evento ? dto.EventoFechaUtc : null;
        a.ImagenesIds = dto.ImagenesIds is { Count: > 0 } ? string.Join(",", dto.ImagenesIds) : null;
    }

    private static string PrimeraLinea(string cuerpo)
    {
        var linea = cuerpo.Split('\n', 2)[0].Trim();
        return linea.Length > 120 ? linea[..120] + "…" : linea;
    }

    private static AnuncioDto Mapear(Anuncio a, int likes, int comentarios, bool yoLike) => new(
        a.Id, a.Titulo, a.Cuerpo, a.Categoria, a.Fijado, a.PublicadoUtc, a.EventoFechaUtc,
        string.IsNullOrWhiteSpace(a.AutorNombre) ? "Administración" : a.AutorNombre,
        string.IsNullOrWhiteSpace(a.ImagenesIds)
            ? Array.Empty<Guid>()
            : a.ImagenesIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList(),
        likes, comentarios, yoLike);
}
