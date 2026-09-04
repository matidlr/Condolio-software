# Setup de desarrollo local

## Requisitos

- .NET 9 SDK
- Node.js + npm
- MySQL 8.0 corriendo como servicio (Windows: servicio `MySQL80`)
- `dotnet-ef` como herramienta global: `dotnet tool install --global dotnet-ef`

## Base de datos

Crear la base y el usuario (una vez):

```sql
CREATE DATABASE condolio;
CREATE USER 'condolio'@'localhost' IDENTIFIED BY 'condolio_pw';
GRANT ALL PRIVILEGES ON condolio.* TO 'condolio'@'localhost';
```

Aplicar las migraciones:

```bash
cd src/Condolio.Api
dotnet-ef database update --no-build
```

(si es la primera vez, primero `dotnet build src/Condolio.Api` desde la raíz)

## Secretos (NO están en git)

`src/Condolio.Api/appsettings.Development.json` está en `.gitignore` — hay que traerlo de la PC
anterior (copiarlo directo, no pegarlo en un chat) o recrearlo con esta forma:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=condolio;User=condolio;Password=condolio_pw;"
  },
  "Jwt": { "Key": "...", "Issuer": "Condolio", "Audience": "Condolio" },
  "SuperAdmin": { "Email": "...", "Password": "..." },
  "Google": { "ClientId": "..." },
  "Smtp": { "Host": "...", "Port": 587, "User": "...", "Password": "...", "From": "...", "FromNombre": "Condolio", "EnableSsl": true }
}
```

Sin `Smtp:Host` configurado, los emails (invitaciones, etc.) se loguean en la consola en vez de
enviarse de verdad (`LogEmailSender`).

Google Sign-In: el mismo Client ID va en `appsettings.Development.json` (`Google:ClientId`) **y**
en `frontend/src/environments/environment.ts` (`googleClientId`). Sin eso, el botón de Google
queda deshabilitado.

## Levantar todo

```bash
# API (puerto 5222)
dotnet run --project src/Condolio.Api --urls http://localhost:5222

# Frontend (puerto 4200), en otra terminal
cd frontend
npm install
npx ng serve --port 4200
```

`frontend/src/environments/environment.ts` tiene `apiUrl = http://localhost:5222/api`.

## Usuarios de prueba

| Email | Password | Rol |
|---|---|---|
| `testadmin@demo.com` | `Demo.pass1` | Administrador — consorcio "San Agustin" |
| `residente.test@demo.com` | `Resi.pass1` | Residente — Rita Residente, unidad 2A de San Agustin |
| el de `SuperAdmin` en appsettings | el de appsettings | SuperAdmin |

## Notas de arquitectura para retomar rápido

- **Multi-tenant**: `ITenantOwned { Guid AdministradorId }` + query filter global por el claim
  `tenantId` del JWT (`ITenantContext` / `HttpTenantContext`).
- **EF + hijos por nav collection** tira `DbUpdateConcurrencyException`. Patrón que funciona:
  ```csharp
  var padre = await _db.Padres.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
  _db.Hijos.Add(new Hijo { PadreId = id, AdministradorId = padre.AdministradorId, ... });
  await _db.SaveChangesAsync(ct);
  ```
- **Fechas/horas naive**: ver la sección correspondiente en `CLAUDE.md` — nunca `.toISOString()`
  en el frontend para mandar fecha/hora al backend.
- **Migración de EF**: build desde la raíz antes de `migrations add` (el cwd se resetea entre
  comandos de shell, así que `cd src/Condolio.Api && dotnet-ef ...` en el mismo comando que un
  `cd` anterior puede fallar con `MSB1009`).
- Áreas de administrador (`AreaAdmin`: Finanzas/Operacion/Seguridad/Comunicacion/Residentes) +
  `[RequiereArea(...)]` / `[RequiereAdminGeneral]` controlan permisos por sección para
  administradores con acceso limitado.

Ver también [expensas-diseno.md](expensas-diseno.md) para el estado del módulo de Expensas.
