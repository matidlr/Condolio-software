using Condolio.Application.Archivos;
using Condolio.Application.Common;
using Condolio.Domain.Archivos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Archivos;

public class AdjuntoService : IAdjuntoService
{
    public const int MaxPorOwner = 5;
    public const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/gif", "application/pdf",
    };

    private readonly CondolioDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ITenantContext _tenant;

    public AdjuntoService(CondolioDbContext db, IFileStorage storage, ITenantContext tenant)
    {
        _db = db;
        _storage = storage;
        _tenant = tenant;
    }

    public async Task<Result<IReadOnlyList<AdjuntoDto>>> ListarAsync(TipoAdjuntoOwner ownerTipo, Guid ownerId, CancellationToken ct = default)
    {
        if (!await OwnerAccesible(ownerTipo, ownerId, ct))
            return Result<IReadOnlyList<AdjuntoDto>>.Fail("Recurso no encontrado.");

        var lista = await _db.Adjuntos
            .Where(a => a.OwnerTipo == ownerTipo && a.OwnerId == ownerId)
            .OrderBy(a => a.CreadoUtc)
            .Select(a => new AdjuntoDto(a.Id, a.NombreArchivo, a.ContentType, a.Tamano, a.EsImagen, a.CreadoUtc))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AdjuntoDto>>.Ok(lista);
    }

    public async Task<Result<AdjuntoDto>> SubirAsync(TipoAdjuntoOwner ownerTipo, Guid ownerId, NuevoAdjunto archivo, CancellationToken ct = default)
    {
        if (!await OwnerAccesible(ownerTipo, ownerId, ct))
            return Result<AdjuntoDto>.Fail("Recurso no encontrado.");
        if (!TiposPermitidos.Contains(archivo.ContentType))
            return Result<AdjuntoDto>.Fail("Solo se permiten imágenes o PDF.");
        if (archivo.Tamano <= 0 || archivo.Tamano > MaxBytes)
            return Result<AdjuntoDto>.Fail("El archivo supera el máximo de 10 MB.");

        var cantidad = await _db.Adjuntos.CountAsync(a => a.OwnerTipo == ownerTipo && a.OwnerId == ownerId, ct);
        if (cantidad >= MaxPorOwner)
            return Result<AdjuntoDto>.Fail($"Máximo {MaxPorOwner} adjuntos.");

        var tenantId = _tenant.AdministradorId ?? Guid.Empty;
        var ext = Path.GetExtension(archivo.NombreArchivo);
        var ruta = Path.Combine(tenantId.ToString(), $"{Guid.CreateVersion7()}{ext}").Replace('\\', '/');
        await _storage.GuardarAsync(ruta, archivo.Contenido, ct);

        var adjunto = new Adjunto
        {
            OwnerTipo = ownerTipo,
            OwnerId = ownerId,
            NombreArchivo = Path.GetFileName(archivo.NombreArchivo),
            ContentType = archivo.ContentType,
            Tamano = archivo.Tamano,
            RutaRelativa = ruta,
            SubidoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
        };
        _db.Adjuntos.Add(adjunto);
        await _db.SaveChangesAsync(ct);

        return Result<AdjuntoDto>.Ok(new AdjuntoDto(
            adjunto.Id, adjunto.NombreArchivo, adjunto.ContentType, adjunto.Tamano, adjunto.EsImagen, adjunto.CreadoUtc));
    }

    public async Task<Result<ArchivoDescarga>> DescargarAsync(Guid adjuntoId, CancellationToken ct = default)
    {
        var adjunto = await _db.Adjuntos.FirstOrDefaultAsync(a => a.Id == adjuntoId, ct);
        if (adjunto is null || !_storage.Existe(adjunto.RutaRelativa))
            return Result<ArchivoDescarga>.Fail("Adjunto no encontrado.");

        return Result<ArchivoDescarga>.Ok(new ArchivoDescarga(
            adjunto.NombreArchivo, adjunto.ContentType, _storage.Abrir(adjunto.RutaRelativa)));
    }

    public async Task<Result<AdjuntoDto>> RenombrarAsync(Guid adjuntoId, string nombre, CancellationToken ct = default)
    {
        var adjunto = await _db.Adjuntos.FirstOrDefaultAsync(a => a.Id == adjuntoId, ct);
        if (adjunto is null) return Result<AdjuntoDto>.Fail("Adjunto no encontrado.");

        var limpio = nombre.Trim();
        if (limpio.Length == 0) return Result<AdjuntoDto>.Fail("El nombre no puede estar vacío.");

        var ext = System.IO.Path.GetExtension(adjunto.NombreArchivo);
        if (!string.IsNullOrEmpty(ext) && !limpio.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            limpio += ext;

        adjunto.NombreArchivo = limpio.Length > 260 ? limpio[..260] : limpio;
        await _db.SaveChangesAsync(ct);
        return Result<AdjuntoDto>.Ok(new AdjuntoDto(
            adjunto.Id, adjunto.NombreArchivo, adjunto.ContentType, adjunto.Tamano, adjunto.EsImagen, adjunto.CreadoUtc));
    }

    public async Task<Result> EliminarAsync(Guid adjuntoId, CancellationToken ct = default)
    {
        var adjunto = await _db.Adjuntos.FirstOrDefaultAsync(a => a.Id == adjuntoId, ct);
        if (adjunto is null) return Result.Fail("Adjunto no encontrado.");

        _storage.Eliminar(adjunto.RutaRelativa);
        _db.Adjuntos.Remove(adjunto);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    /// <summary>Verifica que el owner exista dentro del tenant actual (query filter aplica).</summary>
    private Task<bool> OwnerAccesible(TipoAdjuntoOwner ownerTipo, Guid ownerId, CancellationToken ct) => ownerTipo switch
    {
        TipoAdjuntoOwner.Nota => _db.UnidadNotas.AnyAsync(n => n.Id == ownerId, ct),
        TipoAdjuntoOwner.Incidencia => _db.UnidadIncidencias.AnyAsync(i => i.Id == ownerId, ct),
        TipoAdjuntoOwner.Ticket => _db.Tickets.AnyAsync(t => t.Id == ownerId, ct),
        TipoAdjuntoOwner.Amenidad => _db.Amenidades.AnyAsync(a => a.Id == ownerId, ct),
        _ => Task.FromResult(false),
    };
}
