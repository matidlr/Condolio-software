using Condolio.Application.Common;
using Condolio.Application.Documentos;
using Condolio.Domain.Documentos;
using Condolio.Infrastructure.Archivos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Documentos;

public class DocumentoService : IDocumentoService
{
    private const long TopeAlmacenamiento = 10L * 1024 * 1024 * 1024; // 10 GB
    private const long MaxArchivo = 50L * 1024 * 1024;                 // 50 MB

    private readonly CondolioDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ITenantContext _tenant;

    public DocumentoService(CondolioDbContext db, IFileStorage storage, ITenantContext tenant)
    {
        _db = db;
        _storage = storage;
        _tenant = tenant;
    }

    public async Task<Result<ContenidoDto>> ListarAsync(Guid consorcioId, Guid? carpetaId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<ContenidoDto>.Fail("Consorcio no encontrado.");

        string? carpetaNombre = null;
        if (carpetaId is { } cid)
        {
            carpetaNombre = await _db.CarpetasDocumento.Where(c => c.Id == cid && c.ConsorcioId == consorcioId)
                .Select(c => c.Nombre).FirstOrDefaultAsync(ct);
            if (carpetaNombre is null) return Result<ContenidoDto>.Fail("Carpeta no encontrada.");
        }

        var carpetas = await _db.CarpetasDocumento
            .Where(c => c.ConsorcioId == consorcioId && c.CarpetaPadreId == carpetaId)
            .OrderBy(c => c.Nombre)
            .Select(c => new CarpetaDto(c.Id, c.Nombre, c.CarpetaPadreId, c.Nivel,
                _db.Documentos.Count(d => d.CarpetaId == c.Id) + _db.CarpetasDocumento.Count(x => x.CarpetaPadreId == c.Id)))
            .ToListAsync(ct);

        var docs = await _db.Documentos
            .Where(d => d.ConsorcioId == consorcioId && d.CarpetaId == carpetaId)
            .OrderByDescending(d => d.CreadoUtc)
            .Select(d => new DocumentoDto(d.Id, d.Nombre, d.ContentType, d.Tamano, d.CarpetaId, d.Nivel, d.Categoria, d.Destacado, d.CreadoUtc, d.UltimoAccesoUtc, d.SubidoPorNombre))
            .ToListAsync(ct);

        var usado = await _db.Documentos.Where(d => d.ConsorcioId == consorcioId).SumAsync(d => (long?)d.Tamano, ct) ?? 0;

        return Result<ContenidoDto>.Ok(new ContenidoDto(carpetaId, carpetaNombre, carpetas, docs, usado, TopeAlmacenamiento));
    }

    public async Task<Result<IReadOnlyList<DocumentoDto>>> RecientesAsync(Guid consorcioId, CancellationToken ct = default) =>
        Result<IReadOnlyList<DocumentoDto>>.Ok(await _db.Documentos
            .Where(d => d.ConsorcioId == consorcioId)
            .OrderByDescending(d => d.UltimoAccesoUtc ?? d.CreadoUtc)
            .Take(20).Select(d => new DocumentoDto(d.Id, d.Nombre, d.ContentType, d.Tamano, d.CarpetaId, d.Nivel, d.Categoria, d.Destacado, d.CreadoUtc, d.UltimoAccesoUtc, d.SubidoPorNombre)).ToListAsync(ct));

    public async Task<Result<IReadOnlyList<DocumentoDto>>> DestacadosAsync(Guid consorcioId, CancellationToken ct = default) =>
        Result<IReadOnlyList<DocumentoDto>>.Ok(await _db.Documentos
            .Where(d => d.ConsorcioId == consorcioId && d.Destacado)
            .OrderByDescending(d => d.CreadoUtc).Select(d => new DocumentoDto(d.Id, d.Nombre, d.ContentType, d.Tamano, d.CarpetaId, d.Nivel, d.Categoria, d.Destacado, d.CreadoUtc, d.UltimoAccesoUtc, d.SubidoPorNombre)).ToListAsync(ct));

    public async Task<Result<IReadOnlyList<DocumentoDto>>> PorNivelAsync(Guid consorcioId, NivelAcceso nivel, CancellationToken ct = default) =>
        Result<IReadOnlyList<DocumentoDto>>.Ok(await _db.Documentos
            .Where(d => d.ConsorcioId == consorcioId && d.Nivel == nivel)
            .OrderByDescending(d => d.CreadoUtc).Select(d => new DocumentoDto(d.Id, d.Nombre, d.ContentType, d.Tamano, d.CarpetaId, d.Nivel, d.Categoria, d.Destacado, d.CreadoUtc, d.UltimoAccesoUtc, d.SubidoPorNombre)).ToListAsync(ct));

    public async Task<Result<CarpetaDto>> CrearCarpetaAsync(Guid consorcioId, CrearCarpetaDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<CarpetaDto>.Fail("Consorcio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<CarpetaDto>.Fail("El nombre es obligatorio.");
        if (dto.CarpetaPadreId is { } p && !await _db.CarpetasDocumento.AnyAsync(c => c.Id == p && c.ConsorcioId == consorcioId, ct))
            return Result<CarpetaDto>.Fail("Carpeta padre no encontrada.");

        var c = new CarpetaDocumento
        {
            ConsorcioId = consorcioId, Nombre = dto.Nombre.Trim(),
            CarpetaPadreId = dto.CarpetaPadreId, Nivel = dto.Nivel,
        };
        _db.CarpetasDocumento.Add(c);
        await _db.SaveChangesAsync(ct);
        return Result<CarpetaDto>.Ok(new CarpetaDto(c.Id, c.Nombre, c.CarpetaPadreId, c.Nivel, 0));
    }

    public async Task<Result> RenombrarCarpetaAsync(Guid consorcioId, Guid carpetaId, string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Result.Fail("El nombre es obligatorio.");
        var n = await _db.CarpetasDocumento.Where(c => c.Id == carpetaId && c.ConsorcioId == consorcioId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Nombre, nombre.Trim()), ct);
        return n == 0 ? Result.Fail("Carpeta no encontrada.") : Result.Ok();
    }

    public async Task<Result> MoverCarpetaAsync(Guid consorcioId, Guid carpetaId, Guid? destinoId, CancellationToken ct = default)
    {
        var c = await _db.CarpetasDocumento.FirstOrDefaultAsync(x => x.Id == carpetaId && x.ConsorcioId == consorcioId, ct);
        if (c is null) return Result.Fail("Carpeta no encontrada.");
        if (destinoId == carpetaId) return Result.Fail("No se puede mover una carpeta dentro de sí misma.");
        if (destinoId is { } dst)
        {
            // evitar ciclos: el destino no puede ser un descendiente
            var todas = await _db.CarpetasDocumento.Where(x => x.ConsorcioId == consorcioId)
                .Select(x => new { x.Id, x.CarpetaPadreId }).ToListAsync(ct);
            var actual = dst;
            while (actual != Guid.Empty)
            {
                if (actual == carpetaId) return Result.Fail("No se puede mover dentro de una subcarpeta.");
                var padre = todas.FirstOrDefault(x => x.Id == actual)?.CarpetaPadreId;
                if (padre is null) break;
                actual = padre.Value;
            }
        }
        c.CarpetaPadreId = destinoId;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<CarpetaDto>>> TodasLasCarpetasAsync(Guid consorcioId, CancellationToken ct = default) =>
        Result<IReadOnlyList<CarpetaDto>>.Ok(await _db.CarpetasDocumento
            .Where(c => c.ConsorcioId == consorcioId)
            .OrderBy(c => c.Nombre)
            .Select(c => new CarpetaDto(c.Id, c.Nombre, c.CarpetaPadreId, c.Nivel, 0))
            .ToListAsync(ct));

    public async Task<Result> EliminarCarpetaAsync(Guid consorcioId, Guid carpetaId, CancellationToken ct = default)
    {
        var c = await _db.CarpetasDocumento.FirstOrDefaultAsync(x => x.Id == carpetaId && x.ConsorcioId == consorcioId, ct);
        if (c is null) return Result.Fail("Carpeta no encontrada.");
        if (await _db.CarpetasDocumento.AnyAsync(x => x.CarpetaPadreId == carpetaId, ct)
            || await _db.Documentos.AnyAsync(d => d.CarpetaId == carpetaId, ct))
            return Result.Fail("La carpeta no está vacía.");

        _db.CarpetasDocumento.Remove(c);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<DocumentoDto>> SubirAsync(Guid consorcioId, NuevoDocumento archivo, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<DocumentoDto>.Fail("Consorcio no encontrado.");
        if (archivo.Tamano <= 0 || archivo.Tamano > MaxArchivo)
            return Result<DocumentoDto>.Fail("El archivo supera el tamaño máximo (50 MB).");
        if (archivo.CarpetaId is { } fid && !await _db.CarpetasDocumento.AnyAsync(c => c.Id == fid && c.ConsorcioId == consorcioId, ct))
            return Result<DocumentoDto>.Fail("Carpeta no encontrada.");

        var usado = await _db.Documentos.Where(d => d.ConsorcioId == consorcioId).SumAsync(d => (long?)d.Tamano, ct) ?? 0;
        if (usado + archivo.Tamano > TopeAlmacenamiento)
            return Result<DocumentoDto>.Fail("No hay espacio de almacenamiento suficiente.");

        var ext = Path.GetExtension(archivo.Nombre);
        var ruta = $"documentos/{consorcioId:N}/{Guid.CreateVersion7():N}{ext}";
        await _storage.GuardarAsync(ruta, archivo.Contenido, ct);

        var autor = _tenant.UsuarioId is { } uid
            ? await _db.Users.Where(u => u.Id == uid).Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct)
            : null;

        var d = new Documento
        {
            ConsorcioId = consorcioId,
            CarpetaId = archivo.CarpetaId,
            Nombre = archivo.Nombre.Trim(),
            ContentType = archivo.ContentType,
            Tamano = archivo.Tamano,
            RutaRelativa = ruta,
            Nivel = archivo.Nivel,
            Categoria = archivo.Categoria,
            SubidoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            SubidoPorNombre = autor ?? "Administración",
        };
        _db.Documentos.Add(d);
        await _db.SaveChangesAsync(ct);
        return Result<DocumentoDto>.Ok(ProyectarObj(d));
    }

    public async Task<Result<DocumentoDto>> ActualizarAsync(Guid consorcioId, Guid documentoId, ActualizarDocumentoDto dto, CancellationToken ct = default)
    {
        var d = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == documentoId && x.ConsorcioId == consorcioId, ct);
        if (d is null) return Result<DocumentoDto>.Fail("Documento no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<DocumentoDto>.Fail("El nombre es obligatorio.");
        if (dto.CarpetaId is { } fid && !await _db.CarpetasDocumento.AnyAsync(c => c.Id == fid && c.ConsorcioId == consorcioId, ct))
            return Result<DocumentoDto>.Fail("Carpeta no encontrada.");

        d.Nombre = dto.Nombre.Trim();
        d.Nivel = dto.Nivel;
        d.Categoria = dto.Categoria;
        d.CarpetaId = dto.CarpetaId;
        await _db.SaveChangesAsync(ct);
        return Result<DocumentoDto>.Ok(ProyectarObj(d));
    }

    public async Task<Result> DestacarAsync(Guid consorcioId, Guid documentoId, bool destacar, CancellationToken ct = default)
    {
        var n = await _db.Documentos.Where(d => d.Id == documentoId && d.ConsorcioId == consorcioId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Destacado, destacar), ct);
        return n == 0 ? Result.Fail("Documento no encontrado.") : Result.Ok();
    }

    public async Task<Result<ArchivoDocumento>> DescargarAsync(Guid consorcioId, Guid documentoId, bool registrarDescarga = false, CancellationToken ct = default)
    {
        var d = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == documentoId && x.ConsorcioId == consorcioId, ct);
        if (d is null || !_storage.Existe(d.RutaRelativa)) return Result<ArchivoDocumento>.Fail("Documento no encontrado.");

        _db.DocumentosAcceso.Add(new DocumentoAcceso
        {
            AdministradorId = d.AdministradorId,
            ConsorcioId = consorcioId,
            DocumentoId = documentoId,
            EsDescarga = registrarDescarga,
            UsuarioId = _tenant.UsuarioId ?? string.Empty,
        });
        await _db.SaveChangesAsync(ct);
        await _db.Documentos.Where(x => x.Id == documentoId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UltimoAccesoUtc, DateTime.UtcNow), ct);

        return Result<ArchivoDocumento>.Ok(new ArchivoDocumento(d.Nombre, d.ContentType, _storage.Abrir(d.RutaRelativa)));
    }

    public async Task<Result<AnaliticasDto>> AnaliticasAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<AnaliticasDto>.Fail("Consorcio no encontrado.");

        var docs = await _db.Documentos.AsNoTracking()
            .Where(d => d.ConsorcioId == consorcioId)
            .Select(d => new { d.Id, d.Nombre, d.Categoria, d.Tamano, d.Nivel, d.UltimoAccesoUtc })
            .ToListAsync(ct);

        var accesos = await _db.DocumentosAcceso.AsNoTracking()
            .Where(a => a.ConsorcioId == consorcioId)
            .Select(a => new { a.DocumentoId, a.EsDescarga, a.UsuarioId, a.CreadoUtc })
            .ToListAsync(ct);

        var ahora = DateTime.UtcNow;
        var totalVistas = accesos.Count(a => !a.EsDescarga);
        var totalDescargas = accesos.Count(a => a.EsDescarga);

        var porCategoria = docs
            .GroupBy(d => d.Categoria)
            .Select(g => new CategoriaAggDto(g.Key, g.Count(), g.Sum(x => x.Tamano)))
            .OrderByDescending(c => c.Tamano)
            .ToList();

        var populares = docs
            .Select(d => new DocPopularDto(
                d.Id, d.Nombre, d.Categoria,
                accesos.Count(a => a.DocumentoId == d.Id && !a.EsDescarga),
                accesos.Count(a => a.DocumentoId == d.Id && a.EsDescarga),
                d.UltimoAccesoUtc))
            .OrderByDescending(d => d.Vistas + d.Descargas)
            .ThenByDescending(d => d.UltimoAccesoUtc)
            .Take(10)
            .ToList();

        var desde = (accesos.Count > 0 ? accesos.Min(a => a.CreadoUtc) : ahora).Date;
        if ((ahora.Date - desde).TotalDays > 30) desde = ahora.Date.AddDays(-30);
        var timeline = new List<TimelinePuntoDto>();
        for (var dia = desde; dia <= ahora.Date; dia = dia.AddDays(1))
        {
            var delDia = accesos.Where(a => a.CreadoUtc.Date == dia).ToList();
            timeline.Add(new TimelinePuntoDto(DateOnly.FromDateTime(dia),
                delDia.Count(a => !a.EsDescarga), delDia.Count(a => a.EsDescarga)));
        }

        return Result<AnaliticasDto>.Ok(new AnaliticasDto(
            docs.Count,
            docs.Sum(d => d.Tamano),
            TopeAlmacenamiento,
            totalVistas,
            totalDescargas,
            accesos.Where(a => !a.EsDescarga && a.UsuarioId != string.Empty).Select(a => a.UsuarioId).Distinct().Count(),
            docs.Count(d => d.Nivel != NivelAcceso.Admin),
            accesos.Count(a => a.CreadoUtc >= ahora.AddDays(-30)),
            docs.Count == 0 ? 0 : Math.Round((double)totalDescargas / docs.Count, 1),
            porCategoria,
            populares,
            timeline));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid documentoId, CancellationToken ct = default)
    {
        var d = await _db.Documentos.FirstOrDefaultAsync(x => x.Id == documentoId && x.ConsorcioId == consorcioId, ct);
        if (d is null) return Result.Fail("Documento no encontrado.");
        _storage.Eliminar(d.RutaRelativa);
        _db.Documentos.Remove(d);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static DocumentoDto ProyectarObj(Documento d) => new(
        d.Id, d.Nombre, d.ContentType, d.Tamano, d.CarpetaId, d.Nivel, d.Categoria, d.Destacado,
        d.CreadoUtc, d.UltimoAccesoUtc, string.IsNullOrWhiteSpace(d.SubidoPorNombre) ? "Administración" : d.SubidoPorNombre);
}
