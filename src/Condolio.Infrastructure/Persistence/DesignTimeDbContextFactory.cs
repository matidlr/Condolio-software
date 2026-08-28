using Condolio.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Condolio.Infrastructure.Persistence;

/// <summary>Permite a `dotnet ef` crear el contexto sin DI ni MySQL vivo.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CondolioDbContext>
{
    public CondolioDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CondolioDbContext>()
            .UseMySql("Server=localhost;Database=condolio;User=condolio;Password=condolio_pw;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;
        return new CondolioDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? AdministradorId => null;
        public bool EsSuperAdmin => true;
        public string? UsuarioId => null;
    }
}
