namespace Condolio.Application.Common;

/// <summary>Fuente de tiempo inyectable (testeable).</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
