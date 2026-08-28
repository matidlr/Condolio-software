namespace Condolio.Domain.Common;

/// <summary>Base para todas las entidades persistentes. Id = Guid v7 (secuencial).</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActualizadoUtc { get; set; }
}

/// <summary>
/// Marca una entidad del <b>application plane</b> que pertenece a un tenant (administrador).
/// El <see cref="AdministradorId"/> lo completa el interceptor de EF y lo filtra el query filter global.
/// </summary>
public interface ITenantOwned
{
    Guid AdministradorId { get; set; }
}
