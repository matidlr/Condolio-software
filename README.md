# Condolio

SaaS multi-tenant de administración de consorcios. Backend .NET 9 (Clean Architecture) + frontend Angular 19.

## Arquitectura

Monolito modular con dos planos:

| Plano | Contenido | Actor |
|---|---|---|
| **Control plane** | Alta de tenants, suscripciones (trial 2 meses → Mercado Pago), planes y pricing por unidades, métricas del negocio | SuperAdmin |
| **Application plane** | Consorcios, unidades, expensas, gastos, residentes, etc. | Administrador / Residente |

### Proyectos

```
src/Condolio.Domain          Entidades y reglas puras. Sin dependencias.
src/Condolio.Application      Casos de uso e interfaces (puertos). Depende solo de Domain.
src/Condolio.Infrastructure   EF Core (MySQL), Identity, JWT, Mercado Pago, jobs.
src/Condolio.Api             Controllers, auth, DI. Composition root.
frontend/                     Workspace Angular 19.
```

### Multi-tenancy

Base única. Las entidades del application plane implementan `ITenantOwned` (`AdministradorId`).
`CondolioDbContext` aplica un **query filter global** por tenant, resuelto del claim `tenantId`
del JWT vía `ITenantContext`. SuperAdmin (sin tenant) ve todo.

## Puesta en marcha (dev)

```bash
docker compose up -d                 # MySQL en localhost:3306 (db condolio)
dotnet ef database update -p src/Condolio.Infrastructure -s src/Condolio.Api
dotnet run --project src/Condolio.Api # API en http://localhost:5222 (Swagger en /swagger)
npm start --prefix frontend           # Angular en http://localhost:4200
```

Credenciales dev del SuperAdmin: ver `src/Condolio.Api/appsettings.Development.json`
(`SuperAdmin:Email` / `SuperAdmin:Password`). En producción usar user-secrets / variables de entorno
para `ConnectionStrings:Default`, `Jwt:SecretKey` y `SuperAdmin:Password`.

## Migraciones

```bash
dotnet ef migrations add <Nombre> -p src/Condolio.Infrastructure -s src/Condolio.Api -o Persistence/Migrations
```
