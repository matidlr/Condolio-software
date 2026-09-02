import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CriterioDistribucion, EstadoExtraordinaria, ExpensasService, Extraordinaria,
  ExtraordinariasLista, GuardarExtraordinaria,
} from '../../core/services/expensas.service';

const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

@Component({
  selector: 'app-ex-extraordinarias',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './extraordinarias.component.html',
  styleUrl: './expensas.shared.scss',
})
export class ExtraordinariasComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  data = signal<ExtraordinariasLista | null>(null);
  cargando = signal(true);

  modal = signal(false);
  editId = signal<string | null>(null);
  form = signal<GuardarExtraordinaria>(this.vacio());
  guardando = signal(false);

  meses = MESES.map((m, i) => ({ v: i + 1, t: m }));
  anios = (() => { const y = new Date().getFullYear(); return [y - 1, y, y + 1, y + 2]; })();
  criterios: { v: CriterioDistribucion; t: string }[] = [
    { v: 'PorCoeficiente', t: 'Por coeficiente' },
    { v: 'PartesIguales', t: 'Partes iguales' },
  ];

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  fmt(n: number): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 0 });
  }
  periodo(m: number, a: number): string { return `${MESES[m - 1]} ${a}`; }
  /** Período de la última cuota, dado el inicio y la cantidad de meses de prorrateo. */
  finPeriodo(m: number, a: number, meses: number): { m: number; a: number } {
    const idx = (a * 12 + (m - 1)) + Math.max(1, meses) - 1;
    return { m: (idx % 12) + 1, a: Math.floor(idx / 12) };
  }
  rango(m: number, a: number, meses: number): string {
    if (meses <= 1) return this.periodo(m, a);
    const f = this.finPeriodo(m, a, meses);
    return `${this.periodo(m, a)} → ${this.periodo(f.m, f.a)}`;
  }
  estadoTxt(e: EstadoExtraordinaria): string {
    return e === 'Activa' ? 'Activa' : e === 'Finalizada' ? 'Finalizada' : 'Cancelada';
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.extraordinarias(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar las expensas extraordinarias.'); },
    });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  private vacio(): GuardarExtraordinaria {
    const hoy = new Date();
    return {
      descripcion: '', motivo: '', montoTotal: 0, cantidadCuotas: 1,
      criterioDistribucion: 'PorCoeficiente',
      periodoInicioMes: hoy.getMonth() + 1, periodoInicioAnio: hoy.getFullYear(),
      fechaAprobacion: null, notas: '',
    };
  }
  set<K extends keyof GuardarExtraordinaria>(k: K, v: GuardarExtraordinaria[K]): void {
    this.form.update((f) => ({ ...f, [k]: v }));
  }
  montoCuotaPreview = computed(() => {
    const f = this.form();
    return f.cantidadCuotas <= 1 ? f.montoTotal : Math.round(f.montoTotal / f.cantidadCuotas);
  });
  prorratea = computed(() => this.form().cantidadCuotas > 1);
  rangoPreview = computed(() => {
    const f = this.form();
    return this.rango(f.periodoInicioMes, f.periodoInicioAnio, f.cantidadCuotas);
  });

  setModoProrrateo(prorratear: boolean): void {
    this.set('cantidadCuotas', prorratear ? Math.max(2, this.form().cantidadCuotas) : 1);
  }

  abrirNuevo(): void { this.editId.set(null); this.form.set(this.vacio()); this.modal.set(true); }
  abrirEditar(x: Extraordinaria): void {
    this.editId.set(x.id);
    this.form.set({
      descripcion: x.descripcion, motivo: x.motivo ?? '', montoTotal: x.montoTotal,
      cantidadCuotas: x.cantidadCuotas, criterioDistribucion: x.criterioDistribucion,
      periodoInicioMes: x.periodoInicioMes, periodoInicioAnio: x.periodoInicioAnio,
      fechaAprobacion: x.fechaAprobacion ?? null, notas: x.notas ?? '',
    });
    this.modal.set(true);
  }

  guardar(): void {
    const cid = this.cid();
    const f = this.form();
    if (!cid || !f.descripcion.trim() || f.montoTotal <= 0 || this.guardando()) return;
    this.guardando.set(true);
    const id = this.editId();
    const obs = id ? this.api.actualizarExtraordinaria(cid, id, f) : this.api.crearExtraordinaria(cid, f);
    obs.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.toasts.exito(id ? 'Expensa extraordinaria actualizada' : 'Expensa extraordinaria creada'); this.cargar(cid); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  cambiarEstado(x: Extraordinaria, estado: EstadoExtraordinaria): void {
    const cid = this.cid(); if (!cid) return;
    this.api.estadoExtraordinaria(cid, x.id, estado).subscribe({
      next: () => { this.toasts.exito(`Marcada como ${this.estadoTxt(estado).toLowerCase()}`); this.cargar(cid); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar el estado.'),
    });
  }
}
