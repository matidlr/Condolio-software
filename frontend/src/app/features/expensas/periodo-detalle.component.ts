import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import { UnidadService } from '../../core/services/unidad.service';
import { Unidad } from '../../core/models/consorcio.models';
import {
  AlcanceGasto, CriterioDistribucion, ExpensasService, Extraordinaria, GastoPeriodo,
  GuardarGastoPeriodo, PeriodoDetalle, Proveedor, RubroGasto, TipoRubro,
} from '../../core/services/expensas.service';

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio',
  'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

@Component({
  selector: 'app-ex-periodo-detalle',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './periodo-detalle.component.html',
  styleUrl: './expensas.shared.scss',
})
export class PeriodoDetalleComponent {
  private api = inject(ExpensasService);
  private unidadesApi = inject(UnidadService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  private cid = computed(() => this.consorcios.activoId());
  periodoId = signal<string>(this.route.snapshot.paramMap.get('periodoId') ?? '');

  data = signal<PeriodoDetalle | null>(null);
  cargando = signal(true);

  rubros = signal<RubroGasto[]>([]);
  proveedores = signal<Proveedor[]>([]);
  extraordinarias = signal<Extraordinaria[]>([]);
  unidades = signal<Unidad[]>([]);

  modal = signal(false);
  editId = signal<string | null>(null);
  form = signal<GuardarGastoPeriodo>(this.vacio());
  guardando = signal(false);
  subiendo = signal(false);

  criterios: { v: CriterioDistribucion; t: string }[] = [
    { v: 'PorCoeficiente', t: 'Por coeficiente' },
    { v: 'PartesIguales', t: 'Partes iguales' },
  ];

  constructor() {
    effect(() => {
      const c = this.cid();
      const p = this.periodoId();
      if (c && p) this.cargar(c, p);
    });
  }

  titulo = computed(() => {
    const d = this.data();
    return d ? `${MESES[d.mes - 1]} ${d.anio}` : '';
  });
  abierto = computed(() => this.data()?.estado === 'Abierto');
  fmt(n: number): string { return '$' + (n ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 0 }); }
  nombreRubro(id: string): string { return this.rubros().find((r) => r.id === id)?.nombre ?? '—'; }

  private cargar(cid: string, pid: string): void {
    this.cargando.set(true);
    this.api.periodo(cid, pid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar el período.'); },
    });
    this.api.rubros(cid).subscribe({ next: (r) => this.rubros.set(r) });
    this.api.proveedores(cid).subscribe({ next: (p) => this.proveedores.set(p.proveedores.filter((x) => x.activo)) });
    this.api.extraordinarias(cid).subscribe({ next: (x) => this.extraordinarias.set(x.extraordinarias.filter((e) => e.estado === 'Activa')) });
    this.unidadesApi.listar(cid).subscribe({ next: (u) => this.unidades.set(u) });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c, this.periodoId()); }

  private vacio(): GuardarGastoPeriodo {
    return {
      rubroGastoId: '', proveedorId: null, descripcion: '', monto: 0,
      fecha: new Date().toISOString().slice(0, 10),
      metodoPago: null, cuentaPago: null,
      alcance: 'Todas', criterioDistribucion: 'PorCoeficiente',
      extraordinariaId: null, unidadIds: [],
    };
  }
  set<K extends keyof GuardarGastoPeriodo>(k: K, v: GuardarGastoPeriodo[K]): void {
    this.form.update((f) => ({ ...f, [k]: v }));
  }
  toggleUnidad(id: string): void {
    const cur = new Set(this.form().unidadIds ?? []);
    cur.has(id) ? cur.delete(id) : cur.add(id);
    this.set('unidadIds', [...cur]);
  }

  abrirNuevo(): void {
    this.editId.set(null);
    const f = this.vacio();
    f.rubroGastoId = this.rubros()[0]?.id ?? '';
    this.form.set(f);
    this.modal.set(true);
  }
  abrirEditar(g: GastoPeriodo): void {
    this.editId.set(g.id);
    this.form.set({
      rubroGastoId: g.rubroGastoId, proveedorId: g.proveedorId ?? null, descripcion: g.descripcion,
      monto: g.monto, fecha: g.fecha.slice(0, 10), metodoPago: g.metodoPago ?? null, cuentaPago: g.cuentaPago ?? null,
      alcance: g.alcance, criterioDistribucion: g.criterioDistribucion,
      extraordinariaId: g.extraordinariaId ?? null, unidadIds: [...g.unidadIds],
    });
    this.modal.set(true);
  }

  guardar(): void {
    const cid = this.cid();
    const pid = this.periodoId();
    const f = this.form();
    if (!cid || !f.descripcion.trim() || !f.rubroGastoId || this.guardando()) return;
    if (f.alcance === 'Subconjunto' && (f.unidadIds ?? []).length === 0) {
      this.toasts.error('Elegí al menos una unidad.');
      return;
    }
    this.guardando.set(true);
    const id = this.editId();
    const obs = id ? this.api.actualizarGasto(cid, pid, id, f) : this.api.crearGasto(cid, pid, f);
    obs.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.toasts.exito(id ? 'Gasto actualizado' : 'Gasto agregado'); this.refrescar(); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  eliminar(g: GastoPeriodo): void {
    const cid = this.cid();
    if (!cid || !confirm(`¿Eliminar "${g.descripcion}"?`)) return;
    this.api.eliminarGasto(cid, this.periodoId(), g.id).subscribe({
      next: () => { this.toasts.exito('Gasto eliminado'); this.refrescar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  subirComprobante(g: GastoPeriodo, ev: Event): void {
    const cid = this.cid();
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!cid || !file) return;
    this.subiendo.set(true);
    this.api.subirComprobanteGasto(cid, this.periodoId(), g.id, file).subscribe({
      next: () => { this.subiendo.set(false); this.toasts.exito('Comprobante subido'); this.refrescar(); },
      error: (e) => { this.subiendo.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo subir.'); },
    });
  }
  verComprobante(g: GastoPeriodo): void {
    const cid = this.cid();
    if (cid) window.open(this.api.comprobanteGastoUrl(cid, this.periodoId(), g.id), '_blank');
  }

  reabrir(): void {
    const cid = this.cid();
    if (!cid || !confirm('¿Reabrir el período para editar los gastos?')) return;
    this.api.reabrirPeriodo(cid, this.periodoId()).subscribe({
      next: () => { this.toasts.exito('Período reabierto'); this.refrescar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo reabrir.'),
    });
  }

  tipoLabel(t: TipoRubro): string {
    return t === 'Ordinario' ? 'Ordinario (A)' : t === 'Extraordinario' ? 'Extraordinario (B)' : 'Fondo de reserva';
  }
  volver(): void { this.router.navigate(['/panel/expensas/gastos']); }
}
