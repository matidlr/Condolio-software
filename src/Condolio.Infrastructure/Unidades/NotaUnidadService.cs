using Condolio.Application.Common;
using Condolio.Application.Unidades;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Unidades;

public class NotaUnidadService : INotaUnidadService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IActividadUnidadService _actividad;

    public NotaUnidadService(CondolioDbContext db, ITenantContext tenant, IActividadUnidadService actividad)
    {
        _db = db;
        _tenant = tenant;
        _actividad = actividad;
    }

    private static string Extracto(string texto) =>
        texto.Length <= 80 ? texto : texto[..80] + "…";

    public async Task<Result<IReadOnlyList<NotaUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default)
    {
        if (!await UnidadExiste(consorcioId, unidadId, ct))
            return Result<IReadOnlyList<NotaUnidadDto>>.Fail("Unidad no encontrada.");

        var notas = await (
            from n in _db.UnidadNotas.Where(n => n.UnidadId == unidadId)
            join u in _db.Users on n.AutorUsuarioId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby n.CreadoUtc descending
            select new NotaUnidadDto(
                n.Id,
                n.Texto,
                u != null ? (u.Nombre + " " + u.Apellido).Trim() : "—",
                n.AutorUsuarioId,
                n.CreadoUtc,
                n.ActualizadoUtc))
            .ToListAsync(ct);

        return Result<IReadOnlyList<NotaUnidadDto>>.Ok(notas);
    }

    public async Task<Result<NotaUnidadDto>> AgregarAsync(Guid consorcioId, Guid unidadId, GuardarNotaDto dto, CancellationToken ct = default)
    {
        if (!await UnidadExiste(consorcioId, unidadId, ct))
            return Result<NotaUnidadDto>.Fail("Unidad no encontrada.");
        var texto = dto.Texto?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return Result<NotaUnidadDto>.Fail("La nota no puede estar vacía.");

        var nota = new UnidadNota
        {
            UnidadId = unidadId,
            Texto = texto,
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
        };
        _db.UnidadNotas.Add(nota);
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.NotaAgregada, "Nota agregada", Extracto(texto), ct);

        return await DevolverNota(nota, ct);
    }

    public async Task<Result<NotaUnidadDto>> EditarAsync(Guid consorcioId, Guid unidadId, Guid notaId, GuardarNotaDto dto, CancellationToken ct = default)
    {
        var nota = await _db.UnidadNotas
            .Include(n => n.Unidad)
            .FirstOrDefaultAsync(n => n.Id == notaId && n.UnidadId == unidadId
                && n.Unidad.ConsorcioId == consorcioId, ct);
        if (nota is null) return Result<NotaUnidadDto>.Fail("Nota no encontrada.");

        var texto = dto.Texto?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return Result<NotaUnidadDto>.Fail("La nota no puede estar vacía.");

        nota.Texto = texto;
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.NotaEditada, "Nota editada", Extracto(texto), ct);

        return await DevolverNota(nota, ct);
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, Guid notaId, CancellationToken ct = default)
    {
        var nota = await _db.UnidadNotas
            .Include(n => n.Unidad)
            .FirstOrDefaultAsync(n => n.Id == notaId && n.UnidadId == unidadId
                && n.Unidad.ConsorcioId == consorcioId, ct);
        if (nota is null) return Result.Fail("Nota no encontrada.");

        _db.UnidadNotas.Remove(nota);
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.NotaEliminada, "Nota eliminada", null, ct);
        return Result.Ok();
    }

    private Task<bool> UnidadExiste(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        _db.Unidades.AnyAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);

    private async Task<Result<NotaUnidadDto>> DevolverNota(UnidadNota nota, CancellationToken ct)
    {
        var autor = await _db.Users
            .Where(u => u.Id == nota.AutorUsuarioId)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim())
            .FirstOrDefaultAsync(ct);

        return Result<NotaUnidadDto>.Ok(new NotaUnidadDto(
            nota.Id, nota.Texto, autor ?? "—", nota.AutorUsuarioId, nota.CreadoUtc, nota.ActualizadoUtc));
    }
}
