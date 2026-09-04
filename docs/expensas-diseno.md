# Expensas — diseño del módulo

Estado: **diseño cerrado** (2026-09-03). Pendiente de construir: períodos, gastos del período,
liquidación, pagos y Mercado Pago.

## Contexto Argentina

- No hay "cuota mensual" fija: la expensa de cada unidad se calcula desde los gastos reales del mes.
- Reparto **siempre por coeficiente** de cada unidad (suma ~100). Alcance por gasto: todas o subconjunto.
- Liquidación separada en **Expensas A (ordinarias, paga inquilino)** y **B (extraordinarias / fondo de obras, paga propietario)** en el mismo documento.
- Inflación: los montos de gastos fijos y sueldos se editan mes a mes.

## Ya construido (chunk 1)

- `ConfiguracionExpensas` (vencimientos, recargo 2º venc, tasa mora mensual, fondo de reserva, Mercado Pago token).
- `RubroGasto` (13 base), `Proveedor`, `Empleado` (costo mensual calculado), `GastoFijo` (abonos recurrentes).
- `Extraordinaria` + `ExtraordinariaUnidad` + `CargoUnidad` (cargo individual con vencimiento / estado de pago / nº cuota).
- `Extraordinaria`: wizard 5 pasos (datos / unidades / método+montos Igual|ProporcionalPorCoeficiente|Personalizado / prorrateo en meses / revisión). Genera `CargoUnidad` prorrateados.
- **Morosidad**: semáforo por antigüedad de deuda sobre `CargoUnidad` pendientes; vista tarjetas por piso + tabla + export CSV.

## Chunk 2 — Período y gastos

### PeriodoExpensas
- mes / año, estado **Abierto → Liquidado → Cerrado**.
- Todo lo del mes cuelga de acá.

### GastoPeriodo
Al abrir un período se **precargan automáticamente**:
- Costo de cada **empleado activo** (foto del sueldo + cargas + aguinaldo del catálogo).
- Cada **gasto fijo activo** (foto del monto estimado).

El admin:
- Edita esos montos precargados para el mes (inflación) — no toca el catálogo, el período guarda su propia foto.
- Agrega **gastos variables reales**: descripción, rubro, proveedor, monto real, fecha, **comprobante** (foto o PDF).
- Alcance por gasto: **todas** las unidades (default) o **subconjunto seleccionado** (mismo patrón que extraordinarias). Reparto por coeficiente, renormalizado a la suma de coeficientes del subconjunto.
- Tipo por rubro: ordinario (A) / extraordinario o fondo de obras (B).

## Chunk 3 — Liquidación

Botón "Liquidar período":
1. Valida que todos los coeficientes estén cargados y sumen ~100.
2. Suma los gastos por rubro y por tipo (A / B).
3. Reparte cada gasto entre sus unidades de alcance por coeficiente.
4. Suma **fondo de reserva** (según config) e **intereses por mora** sobre el saldo impago del período anterior.
5. Genera un `CargoUnidad` por unidad (origen `ExpensaOrdinaria`), separando importe A e importe B.
6. Produce la **expensa de cada unidad** (PDF: detalle de gastos del consorcio + prorrateo de esa unidad + saldo anterior + total A / total B / total a pagar + vencimientos).
7. Período pasa a **Liquidado**.

### Estado de cuenta por unidad
Ledger de `CargoUnidad` + `Pago` aplicados. Saldo capital + intereses devengados.

## Chunk 4 — Pagos

### Formas de registrar un pago

| Forma | Quién carga | Estado inicial |
|---|---|---|
| Mercado Pago | webhook automático | `Acreditado` |
| Declaración del residente (transferencia, depósito) | residente desde el portal | `EnRevision` |
| Carga directa del admin (efectivo en oficina) | admin | `Aprobado` |

### Circuito declaración del residente
1. Residente en su estado de cuenta → "Informar pago": monto, fecha, método, **adjunta comprobante(s)** (transferencia + factura). Comprobante **obligatorio** para pagos que no son MP.
2. Pago queda `EnRevision` — no baja la deuda.
3. Admin → cola "Pagos por aprobar" → ve adjuntos → **Aprobar** (se aplica a cargos, más viejo primero, baja deuda, avisa al residente) o **Rechazar** con motivo (deuda queda igual, avisa al residente).

### Reglas
- Solo `Acreditado` / `Aprobado` toca los cargos.
- **Pago parcial permitido**: se aplica al cargo más viejo; el cargo a medias pasa a `PagadoParcial` y el saldo sigue generando interés.
- **Tope = deuda actual** (capital + intereses devengados recalculados al momento de pagar). **No se puede pagar de más**, no hay saldo a favor.

### Modelo
- `Pago` (unidadId, monto, fecha, metodo, estado, motivoRechazo?, mpPaymentId?, revisadoPor?, revisadoUtc?)
- `PagoComprobante` (pagoId, archivoRuta, tipo: ComprobanteTransferencia / Factura / Otro) — varios por pago
- `AsignacionPago` (pagoId → cargoUnidadId, monto)

## Chunk 5 — Mercado Pago

- Cuenta **una por consorcio** (token del admin en `ConfiguracionExpensas`). Todos los pagos caen ahí.
- Al liquidar, por cada unidad: crea una **preference** de Checkout Pro (monto = expensa exacta, `external_reference` = períodoId + unidadId, vencimiento = 2º venc).
- El backend genera el **QR (PNG)** del `init_point`. Se muestra en el portal, se manda por email y se imprime en el PDF de la expensa.
- Residente escanea con la cámara → checkout MP con monto fijo (no editable) → paga con **cualquier tarjeta/cuenta o efectivo (Rapipago/Pago Fácil)**, sin necesidad de cuenta MP → webhook.
- **Webhook** `POST /api/mercadopago/webhook` (público): verifica el pago contra la API de MP (no confía en el body), idempotente, crea `Pago` Acreditado, lo aplica a los cargos.
- **Pago parcial**: no usa el QR de la expensa; va por el portal, el residente elige el monto y se crea una preference por ese importe.
- Si no paga y llega el mes siguiente: al liquidar se genera un **QR nuevo** con capital + intereses.
- Comisión MP estándar según medio (débito/saldo más barato que crédito).

## App del residente

- El portal (`/portal/*`) ya existe y es mobile-first (bottom tab bar, safe-area).
- Camino: PWA (manifest + service worker) ahora → **Capacitor** para App Store / Play Store después. Todo Angular, sin reescribir.
- Pantalla nueva principal: **Expensas del residente** (estado de cuenta, expensa del mes, QR/pagar, historial, informar pago manual).
