import { Component, computed, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import { UnidadService } from '../../core/services/unidad.service';
import { Unidad } from '../../core/models/consorcio.models';
import {
  CATEGORIAS_EXTRAORDINARIA, CategoriaExtraordinaria, ExpensasService,
  GuardarExtraordinaria, MetodoReparto,
} from '../../core/services/expensas.service';

type Capturar = 'porUnidad' | 'total';

@Component({
  selector: 'app-cuota-extra-wizard',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './cuota-extraordinaria-wizard.component.html',
  styleUrl: './expensas.shared.scss',
})
export class CuotaExtraordinariaWizardComponent {
  private api = inject(ExpensasService);
  private unidadesApi = inject(UnidadService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  cerrar = output<void>();
  creada = output<void>();

  categorias = CATEGORIAS_EXTRAORDINARIA;
  private cid = () => this.consorcios.activoId();

  paso = signal(1);
  guardando = signal(false);

  unidades = signal<Unidad[]>([]);
  cargandoUnidades = signal(true);

  // paso 1
  titulo = signal('');
  descripcion = signal('');
  categoria = signal<CategoriaExtraordinaria>('Otro');
  fechaInicio = signal(new Date().toISOString().slice(0, 10));
  fechaVencimiento = signal<string>('');

  // paso 2
  seleccion = signal<Set<string>>(new Set());
  busqueda = signal('');

  // paso 3
  metodo = signal<MetodoReparto>('Igual');
  capturar = signal<Capturar>('porUnidad');
  montoBase = signal(0);
  overrides = signal<Map<string, number>>(new Map());

  // paso 4
  cantidadMeses = signal(1);

  // paso 5
  confirmado = signal(false);

  constructor() {
    const c = this.cid();
    if (c) {
      this.unidadesApi.listar(c).subscribe({
        next: (u) => { this.unidades.set(u); this.cargandoUnidades.set(false); },
        error: () => { this.cargandoUnidades.set(false); this.toasts.error('No pudimos cargar las unidades.'); },
      });
    }
  }

  fmt(n: number): string { return '$' + (n ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  nombreCategoria(v: CategoriaExtraordinaria): string { return this.categorias.find((c) => c.v === v)?.t ?? v; }
  nombreMetodo(m: MetodoReparto): string {
    return m === 'Igual' ? 'Igual' : m === 'ProporcionalPorCoeficiente' ? 'Proporcional por coeficiente' : 'Personalizado';
  }
  residentes(u: Unidad): number {
    return (u.propietarios?.length ?? 0) + (u.inquilinos?.length ?? 0) + (u.gestores?.length ?? 0);
  }

  // ---- paso 2: selección ----
  unidadesFiltradas = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return this.unidades().filter((u) => !q || u.nombre.toLowerCase().includes(q));
  });
  seleccionadas = computed(() => this.unidades().filter((u) => this.seleccion().has(u.id)));
  toggleUnidad(id: string): void {
    const s = new Set(this.seleccion());
    s.has(id) ? s.delete(id) : s.add(id);
    this.seleccion.set(s);
    this.overrides.set(new Map());
  }
  seleccionarTodas(): void {
    this.seleccion.set(new Set(this.unidadesFiltradas().map((u) => u.id)));
    this.overrides.set(new Map());
  }
  limpiarSeleccion(): void { this.seleccion.set(new Set()); this.overrides.set(new Map()); }

  // ---- paso 3: montos ----
  private montoCalculado(u: Unidad): number {
    const sel = this.seleccionadas();
    const n = sel.length || 1;
    const base = this.montoBase() || 0;
    if (this.metodo() === 'Igual') {
      return this.capturar() === 'porUnidad' ? base : Math.round((base / n) * 100) / 100;
    }
    if (this.metodo() === 'ProporcionalPorCoeficiente') {
      const sumCoef = sel.reduce((a, x) => a + (x.coeficiente ?? 0), 0);
      if (sumCoef > 0) return Math.round(((base * (u.coeficiente ?? 0)) / sumCoef) * 100) / 100;
      return Math.round((base / n) * 100) / 100; // sin coeficientes → parejo
    }
    return 0; // personalizado arranca en 0
  }
  montoUnidad(u: Unidad): number {
    const ov = this.overrides().get(u.id);
    return ov ?? this.montoCalculado(u);
  }
  setMontoUnidad(id: string, v: number): void {
    const m = new Map(this.overrides());
    m.set(id, Math.max(0, v || 0));
    this.overrides.set(m);
  }
  montosEditados = computed(() => this.overrides().size);
  totalACobrar = computed(() => this.seleccionadas().reduce((a, u) => a + this.montoUnidad(u), 0));
  rangoPorUnidad = computed(() => {
    const ms = this.seleccionadas().map((u) => this.montoUnidad(u)).filter((m) => m > 0);
    if (!ms.length) return { min: 0, max: 0 };
    return { min: Math.min(...ms), max: Math.max(...ms) };
  });
  faltaMonto(u: Unidad): boolean { return this.montoUnidad(u) <= 0; }

  // ---- paso 4 ----
  get prorratea(): boolean { return this.cantidadMeses() > 1; }
  setProrrateo(p: boolean): void { this.cantidadMeses.set(p ? Math.max(2, this.cantidadMeses()) : 1); }
  montoPorMes = computed(() => {
    const t = this.totalACobrar();
    return this.cantidadMeses() <= 1 ? t : Math.round((t / this.cantidadMeses()) * 100) / 100;
  });

  // ---- navegación ----
  puedeSiguiente = computed(() => {
    switch (this.paso()) {
      case 1: return this.titulo().trim().length > 0 && !!this.fechaInicio();
      case 2: return this.seleccion().size > 0;
      case 3: return this.totalACobrar() > 0 && this.seleccionadas().every((u) => this.montoUnidad(u) > 0);
      case 4: return this.cantidadMeses() >= 1;
      default: return true;
    }
  });
  atras(): void { if (this.paso() > 1) this.paso.set(this.paso() - 1); }
  siguiente(): void { if (this.puedeSiguiente() && this.paso() < 5) this.paso.set(this.paso() + 1); }

  crear(): void {
    const cid = this.cid();
    if (!cid || !this.confirmado() || this.guardando()) return;
    const cargos = this.seleccionadas()
      .map((u) => ({ unidadId: u.id, montoAsignado: this.montoUnidad(u) }))
      .filter((c) => c.montoAsignado > 0);
    const dto: GuardarExtraordinaria = {
      titulo: this.titulo().trim(),
      descripcion: this.descripcion().trim() || null,
      categoria: this.categoria(),
      fechaInicio: this.fechaInicio(),
      fechaVencimiento: this.fechaVencimiento() || null,
      metodoReparto: this.metodo(),
      cantidadMeses: this.cantidadMeses(),
      notas: null,
      cargos,
    };
    this.guardando.set(true);
    this.api.crearExtraordinaria(cid, dto).subscribe({
      next: () => { this.guardando.set(false); this.toasts.exito('Cuota extraordinaria creada'); this.creada.emit(); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo crear.'); },
    });
  }
}
