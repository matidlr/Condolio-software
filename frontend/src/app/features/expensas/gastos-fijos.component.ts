import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CriterioDistribucion, Empleado, ExpensasService, GastoFijo, GastosFijosResumen,
  GuardarEmpleado, GuardarGastoFijo, Proveedor, RubroGasto,
} from '../../core/services/expensas.service';

@Component({
  selector: 'app-ex-gastos-fijos',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './gastos-fijos.component.html',
  styleUrl: './expensas.shared.scss',
})
export class GastosFijosComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  data = signal<GastosFijosResumen | null>(null);
  rubros = signal<RubroGasto[]>([]);
  proveedores = signal<Proveedor[]>([]);
  cargando = signal(true);

  // --- empleado modal ---
  modalEmp = signal(false);
  editEmpId = signal<string | null>(null);
  formEmp = signal<GuardarEmpleado>(this.empVacio());
  guardandoEmp = signal(false);

  // --- gasto modal ---
  modalGasto = signal(false);
  editGastoId = signal<string | null>(null);
  formGasto = signal<GuardarGastoFijo>(this.gastoVacio());
  guardandoGasto = signal(false);

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  fmt(n: number): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 0 });
  }
  nombreRubro(id: string | null | undefined): string {
    return this.rubros().find((r) => r.id === id)?.nombre ?? '—';
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.gastosFijos(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar los gastos fijos.'); },
    });
    this.api.rubros(cid).subscribe({ next: (r) => this.rubros.set(r) });
    this.api.proveedores(cid).subscribe({ next: (p) => this.proveedores.set(p.proveedores.filter((x) => x.activo)) });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  // ================= Empleados =================

  private empVacio(): GuardarEmpleado {
    return {
      nombre: '', apellido: '', cuil: '', categoria: '', sueldoBasico: 0, cargasSocialesPct: 0,
      provisionaAguinaldo: true, otrosConceptosMensuales: 0, rubroGastoId: null, fechaIngreso: null, notas: '',
    };
  }
  setEmp<K extends keyof GuardarEmpleado>(k: K, v: GuardarEmpleado[K]): void {
    this.formEmp.update((f) => ({ ...f, [k]: v }));
  }
  previewCostoEmp = computed(() => {
    const f = this.formEmp();
    const cargas = Math.round((f.sueldoBasico * f.cargasSocialesPct) / 100);
    const aguinaldo = f.provisionaAguinaldo ? Math.round(f.sueldoBasico / 12) : 0;
    return (f.sueldoBasico || 0) + cargas + (f.otrosConceptosMensuales || 0) + aguinaldo;
  });

  abrirNuevoEmp(): void { this.editEmpId.set(null); this.formEmp.set(this.empVacio()); this.modalEmp.set(true); }
  abrirEditarEmp(e: Empleado): void {
    this.editEmpId.set(e.id);
    this.formEmp.set({
      nombre: e.nombre, apellido: e.apellido, cuil: e.cuil ?? '', categoria: e.categoria ?? '',
      sueldoBasico: e.sueldoBasico, cargasSocialesPct: e.cargasSocialesPct,
      provisionaAguinaldo: e.provisionaAguinaldo, otrosConceptosMensuales: e.otrosConceptosMensuales,
      rubroGastoId: e.rubroGastoId ?? null, fechaIngreso: e.fechaIngreso ?? null, notas: e.notas ?? '',
    });
    this.modalEmp.set(true);
  }
  guardarEmp(): void {
    const cid = this.cid();
    const f = this.formEmp();
    if (!cid || !f.nombre.trim() || !f.apellido.trim() || this.guardandoEmp()) return;
    this.guardandoEmp.set(true);
    const id = this.editEmpId();
    const obs = id ? this.api.actualizarEmpleado(cid, id, f) : this.api.crearEmpleado(cid, f);
    obs.subscribe({
      next: () => { this.guardandoEmp.set(false); this.modalEmp.set(false); this.toasts.exito(id ? 'Empleado actualizado' : 'Empleado agregado'); this.cargar(cid); },
      error: (e) => { this.guardandoEmp.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }
  toggleEmp(e: Empleado): void {
    const cid = this.cid(); if (!cid) return;
    this.api.estadoEmpleado(cid, e.id, !e.activo).subscribe({
      next: () => this.cargar(cid),
      error: (x) => this.toasts.error(x?.error?.message ?? 'No se pudo cambiar el estado.'),
    });
  }

  // ================= Otros gastos fijos =================

  private gastoVacio(): GuardarGastoFijo {
    return { descripcion: '', rubroGastoId: '', proveedorId: null, montoEstimado: 0, criterioDistribucion: 'PorCoeficiente', notas: '' };
  }
  setGasto<K extends keyof GuardarGastoFijo>(k: K, v: GuardarGastoFijo[K]): void {
    this.formGasto.update((f) => ({ ...f, [k]: v }));
  }

  abrirNuevoGasto(): void {
    this.editGastoId.set(null);
    const g = this.gastoVacio();
    g.rubroGastoId = this.rubros()[0]?.id ?? '';
    this.formGasto.set(g);
    this.modalGasto.set(true);
  }
  abrirEditarGasto(g: GastoFijo): void {
    this.editGastoId.set(g.id);
    this.formGasto.set({
      descripcion: g.descripcion, rubroGastoId: g.rubroGastoId, proveedorId: g.proveedorId ?? null,
      montoEstimado: g.montoEstimado, criterioDistribucion: g.criterioDistribucion, notas: g.notas ?? '',
    });
    this.modalGasto.set(true);
  }
  guardarGasto(): void {
    const cid = this.cid();
    const f = this.formGasto();
    if (!cid || !f.descripcion.trim() || !f.rubroGastoId || this.guardandoGasto()) return;
    this.guardandoGasto.set(true);
    const id = this.editGastoId();
    const obs = id ? this.api.actualizarGastoFijo(cid, id, f) : this.api.crearGastoFijo(cid, f);
    obs.subscribe({
      next: () => { this.guardandoGasto.set(false); this.modalGasto.set(false); this.toasts.exito(id ? 'Gasto actualizado' : 'Gasto agregado'); this.cargar(cid); },
      error: (e) => { this.guardandoGasto.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }
  toggleGasto(g: GastoFijo): void {
    const cid = this.cid(); if (!cid) return;
    this.api.estadoGastoFijo(cid, g.id, !g.activo).subscribe({
      next: () => this.cargar(cid),
      error: (x) => this.toasts.error(x?.error?.message ?? 'No se pudo cambiar el estado.'),
    });
  }

  criterios: { v: CriterioDistribucion; t: string }[] = [
    { v: 'PorCoeficiente', t: 'Por coeficiente' },
    { v: 'PartesIguales', t: 'Partes iguales' },
  ];
}
