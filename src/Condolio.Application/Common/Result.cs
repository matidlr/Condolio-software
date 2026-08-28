namespace Condolio.Application.Common;

/// <summary>Resultado de un caso de uso sin valor de retorno.</summary>
public readonly record struct Result(bool Exito, string? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

/// <summary>Resultado de un caso de uso con valor.</summary>
public readonly record struct Result<T>(bool Exito, T? Valor, string? Error)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}
