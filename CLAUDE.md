# Condolio

SaaS multi-tenant de administración de consorcios para Argentina. Backend **.NET 9** (Clean
Architecture: Domain/Application/Infrastructure/Api, EF Core 9 + Pomelo MySQL). Frontend
**Angular 19** (standalone components, signals, `@if`/`@for`/`@switch`).

Plataforma de referencia que se va replicando pantalla por pantalla (adaptado a Argentina/ARS/
Mercado Pago): **Koti** (koti.mx).

**Tres actores:** Super administrador (dueño del SaaS) · Administrador de consorcio · Residente
(unidad). Modelo de negocio: 2 meses gratis, luego suscripción mensual por Mercado Pago según
cantidad de unidades ($700 ARS/unidad).

## Cómo correr en local

Ver [docs/dev-setup.md](docs/dev-setup.md) — setup de MySQL, secretos, y comandos para levantar
API + frontend.

## Diseño en curso

El módulo de **Expensas** (períodos, gastos, liquidación, extraordinarias, morosidad, pagos,
Mercado Pago) tiene su diseño completo en [docs/expensas-diseno.md](docs/expensas-diseno.md).
Leelo antes de tocar nada de `Condolio.Domain/Expensas` — ahí están las decisiones ya cerradas
con el usuario (modelo de liquidación A/B, reparto por coeficiente, pagos parciales, etc.) para
no tener que re-preguntarlas.

## Convenciones importantes

- **Migraciones EF** (desde la raíz del repo):
  ```bash
  dotnet build src/Condolio.Api -v q -clp:ErrorsOnly
  cd src/Condolio.Api && dotnet-ef migrations add NombreMigracion --no-build -p ../Condolio.Infrastructure -s .
  cd ../.. && dotnet build src/Condolio.Api -v q -clp:ErrorsOnly
  cd src/Condolio.Api && dotnet-ef database update --no-build
  ```
  El cwd se resetea entre comandos de shell — si un `cd ... && dotnet-ef ...` falla con `MSB1009`,
  es porque el build se corrió desde el directorio equivocado.

- **EF Core + hijos vía nav collection**: `padre.Hijos.Add(x)` + `SaveChangesAsync()` tira
  `DbUpdateConcurrencyException`. Usar `AsNoTracking()` para leer el padre y `DbSet.Add` directo
  con la FK explícita. Ver `docs/dev-setup.md`.

- **Fechas/horas**: todo el sistema las trata como hora local de Argentina, **naive** (sin
  zona) — MySQL borra el `Z`. El frontend **nunca** manda `.toISOString()` (corre -3h). Ver
  `docs/dev-setup.md`.

- **Angular `@if`/`@else if` con `as`**: `@else if (x(); as y)` es un error de compilación
  (NG5002). Envolver: `@else if (x()) { @if (x()!; as y) { ... } }`.

- `Result` / `Result<T>` (`Condolio.Application.Common`): la propiedad de éxito es `Exito`
  (no `Exitoso`).

- Diseño frontend: tokens CSS en `frontend/src/styles.scss` (`--c-navy-800`, `--c-primary`,
  `--c-surface`, `--c-border`, `--c-text-muted`, etc.), clases `.btn`, `.btn--primary`, `.u-btn`.

## Git

Commits en español, cuerpo termina con `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`.
Push a `origin/main` (repo `matidlr/Condolio-software`).
