using Microsoft.Extensions.Configuration;

namespace Condolio.Infrastructure.Archivos;

public interface IFileStorage
{
    Task<string> GuardarAsync(string rutaRelativa, Stream contenido, CancellationToken ct = default);
    Stream Abrir(string rutaRelativa);
    void Eliminar(string rutaRelativa);
    bool Existe(string rutaRelativa);
}

/// <summary>Storage local en disco bajo <c>Storage:BasePath</c>.</summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration config)
    {
        _basePath = config["Storage:BasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "archivos");
        Directory.CreateDirectory(_basePath);
    }

    private string Resolver(string rutaRelativa)
    {
        var full = Path.GetFullPath(Path.Combine(_basePath, rutaRelativa));
        if (!full.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ruta fuera del área de storage.");
        return full;
    }

    public async Task<string> GuardarAsync(string rutaRelativa, Stream contenido, CancellationToken ct = default)
    {
        var full = Resolver(rutaRelativa);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None);
        await contenido.CopyToAsync(fs, ct);
        return rutaRelativa;
    }

    public Stream Abrir(string rutaRelativa) =>
        new FileStream(Resolver(rutaRelativa), FileMode.Open, FileAccess.Read, FileShare.Read);

    public void Eliminar(string rutaRelativa)
    {
        var full = Resolver(rutaRelativa);
        if (File.Exists(full)) File.Delete(full);
    }

    public bool Existe(string rutaRelativa) => File.Exists(Resolver(rutaRelativa));
}
