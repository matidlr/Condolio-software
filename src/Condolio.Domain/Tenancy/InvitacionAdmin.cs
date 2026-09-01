using Condolio.Domain.Common;

namespace Condolio.Domain.Tenancy;

public enum EstadoInvitacionAdmin
{
    Pendiente = 0,
    Aceptada = 1,
    Cancelada = 2,
}

/// <summary>
/// Invitación a administrar una cuenta (tenant) enviada a un correo que todavía no tiene usuario.
/// Cuando esa persona se registra, se acepta automáticamente y se une como co-administrador
/// en lugar de crear un tenant nuevo.
/// </summary>
public class InvitacionAdmin : Entity
{
    public Guid AdministradorId { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool EsGeneral { get; set; } = true;
    public string AreasCsv { get; set; } = string.Empty;

    public EstadoInvitacionAdmin Estado { get; set; } = EstadoInvitacionAdmin.Pendiente;

    public string InvitadoPorUsuarioId { get; set; } = string.Empty;

    public DateTime ExpiraUtc { get; set; } = DateTime.UtcNow.AddDays(14);
}
