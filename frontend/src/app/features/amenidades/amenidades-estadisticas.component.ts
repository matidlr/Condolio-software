import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ReservaService } from '../../core/services/reserva.service';
import { AmenidadService } from '../../core/services/amenidad.service';
import { ToastService } from '../../core/services/toast.service';
import { Reserva } from '../../core/models/reserva.models';

type Periodo = 'semana' | 'mes' | 'trimestre' | 'anio';

@Component({
  selector: 'app-amenidades-estadisticas',
  standalone: true,
  templateUrl: './amenidades-estadisticas.component.html',
  styleUrl: './amenidades.component.scss',
})
export class AmenidadesEstadisticasComponent {
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(ReservaService);
  private amenidadesApi = inject(AmenidadService);
  private toasts = inject(ToastService);

  cargando = signal(true);
  periodo = signal<Periodo>('mes');
  reservas = signal<Reserva[]>([]);
  pendientesGlobal = signal(0);
  amenidadNombres = signal<string[]>([]);

  readonly periodos: { k: Periodo; l: string }[] = [
    { k: 'semana', l: 'Semana' }, { k: 'mes', l: 'Mes' },
    { k: 'trimestre', l: 'Trimestre' }, { k: 'anio', l: 'Anual' },
  ];
  readonly diasSemana = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
  readonly horasEje = [0, 3, 6, 9, 12, 15, 18, 21];
  readonly bucketsAprob = ['<1h', '1-4h', '4-12h', '12-24h', '1-2d', '>2d'];

  readonly W = 520; readonly H = 190; readonly PAD = 26;

  private consorcioId = computed(() => this.consorcios.activoId());

  private dias(): number {
    return { semana: 7, mes: 30, trimestre: 90, anio: 365 }[this.periodo()];
  }
  private desde(): Date {
    const d = new Date(); d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() - (this.dias() - 1));
    return d;
  }

  constructor() {
    effect(() => {
      const id = this.consorcioId();
      if (!id) return;
      this.periodo();
      this.cargar(id);
    });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    const desde = this.desde();
    const hasta = new Date(); hasta.setDate(hasta.getDate() + 366);
    forkJoin([
      this.api.listar(cid, desde.toISOString(), hasta.toISOString()),
      this.amenidadesApi.listar(cid),
    ]).subscribe({
      next: ([lista, ams]) => {
        this.reservas.set(lista.reservas);
        this.pendientesGlobal.set(lista.pendientes);
        this.amenidadNombres.set(ams.amenidades.map((a) => a.nombre));
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las estadísticas.'); this.cargando.set(false); },
    });
  }

  // ---- KPIs ----
  total = computed(() => this.reservas().length);
  aprobadas = computed(() => this.reservas().filter((r) => r.estado === 'Confirmada'));
  rechazadas = computed(() => this.reservas().filter((r) => r.estado === 'Rechazada'));
  resueltas = computed(() => this.aprobadas().length + this.rechazadas().length);

  tasaAprobacion = computed(() =>
    this.resueltas() ? Math.round((this.aprobadas().length / this.resueltas()) * 100) : 0);
  tasaRechazo = computed(() =>
    this.resueltas() ? Math.round((this.rechazadas().length / this.resueltas()) * 100) : 0);

  ingresos = computed(() => this.aprobadas().reduce((s, r) => s + (r.importe ?? 0), 0));

  tiempoPromAprob = computed(() => {
    const conT = this.reservas().filter((r) => r.resueltaUtc);
    if (!conT.length) return null;
    const min = conT.reduce((s, r) =>
      s + (new Date(r.resueltaUtc!).getTime() - new Date(r.creadoUtc).getTime()) / 60000, 0) / conT.length;
    if (min < 60) return `${Math.round(min)}m`;
    if (min < 1440) return `${(min / 60).toFixed(1)}h`;
    return `${(min / 1440).toFixed(1)}d`;
  });

  masPopular = computed(() => {
    const c = new Map<string, number>();
    for (const r of this.reservas()) c.set(r.amenidadNombre, (c.get(r.amenidadNombre) ?? 0) + 1);
    let top = '—', n = 0;
    for (const [k, v] of c) if (v > n) { top = k; n = v; }
    return top;
  });

  tasaUtilizacion = computed(() => {
    // horas reservadas (aprobadas) sobre una capacidad estimada de 12h/día/amenidad
    const horasReservadas = this.aprobadas().reduce((s, r) =>
      s + (new Date(r.fin).getTime() - new Date(r.inicio).getTime()) / 3_600_000, 0);
    const capacidad = Math.max(1, this.amenidadNombres().length) * 12 * this.dias();
    return Math.min(100, Math.round((horasReservadas / capacidad) * 100));
  });

  // ---- Tendencias (línea por estado) ----
  serieTendencias = computed(() => {
    const total = this.dias();
    const paso = total <= 45 ? 1 : 7;
    const base = this.desde();
    const cubos: { fecha: Date; label: string; apr: number; pen: number; rec: number }[] = [];
    for (let i = 0; i < total; i += paso) {
      const f = new Date(base); f.setDate(base.getDate() + i);
      cubos.push({ fecha: f, label: f.toLocaleDateString('es-AR', { day: '2-digit', month: 'short' }), apr: 0, pen: 0, rec: 0 });
    }
    const pasoMs = paso * 86_400_000;
    for (const r of this.reservas()) {
      const idx = Math.floor((new Date(r.inicio).getTime() - cubos[0]?.fecha.getTime()) / pasoMs);
      if (idx < 0 || idx >= cubos.length) continue;
      if (r.estado === 'Confirmada') cubos[idx].apr++;
      else if (r.estado === 'Pendiente') cubos[idx].pen++;
      else if (r.estado === 'Rechazada') cubos[idx].rec++;
    }
    const max = Math.max(1, ...cubos.flatMap((c) => [c.apr, c.pen, c.rec]));
    const innerW = this.W - this.PAD * 2, innerH = this.H - this.PAD * 2;
    const x = (i: number) => this.PAD + (cubos.length <= 1 ? innerW / 2 : (innerW * i) / (cubos.length - 1));
    const y = (v: number) => this.PAD + innerH - (v / max) * innerH;
    const linea = (sel: (c: typeof cubos[number]) => number) =>
      cubos.map((c, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(1)} ${y(sel(c)).toFixed(1)}`).join(' ');
    return {
      cubos, max,
      lineaApr: linea((c) => c.apr),
      lineaPen: linea((c) => c.pen),
      lineaRec: linea((c) => c.rec),
      ticks: cubos.map((c, i) => ({ label: c.label, x: x(i), mostrar: i % (cubos.length > 10 ? 3 : 1) === 0 })),
      ejeY: [0, 1, 2, 3, 4].map((k) => {
        const v = Math.ceil((max * k) / 4);
        return { v, y: y(v) };
      }),
    };
  });

  // ---- Ingresos por amenidad ----
  ingresosPorAmenidad = computed(() => {
    const c = new Map<string, number>();
    for (const r of this.aprobadas()) if (r.importe) c.set(r.amenidadNombre, (c.get(r.amenidadNombre) ?? 0) + r.importe);
    const arr = [...c.entries()].map(([nombre, monto]) => ({ nombre, monto })).sort((a, b) => b.monto - a.monto);
    const max = Math.max(1, ...arr.map((x) => x.monto));
    return arr.map((x) => ({ ...x, pct: (x.monto / max) * 100 }));
  });

  // ---- Reservaciones por amenidad (stacked) ----
  reservasPorAmenidad = computed(() => {
    const m = new Map<string, { apr: number; pen: number; rec: number }>();
    for (const n of this.amenidadNombres()) m.set(n, { apr: 0, pen: 0, rec: 0 });
    for (const r of this.reservas()) {
      const e = m.get(r.amenidadNombre) ?? { apr: 0, pen: 0, rec: 0 };
      if (r.estado === 'Confirmada') e.apr++;
      else if (r.estado === 'Pendiente') e.pen++;
      else if (r.estado === 'Rechazada') e.rec++;
      m.set(r.amenidadNombre, e);
    }
    const arr = [...m.entries()].map(([nombre, v]) => ({ nombre, ...v, tot: v.apr + v.pen + v.rec }));
    const max = Math.max(1, ...arr.map((x) => x.tot));
    return arr.filter((x) => x.tot > 0).map((x) => ({
      ...x,
      wApr: (x.apr / max) * 100, wPen: (x.pen / max) * 100, wRec: (x.rec / max) * 100,
    }));
  });

  // ---- Heatmap día × hora ----
  heatmap = computed(() => {
    const grid: number[][] = Array.from({ length: 7 }, () => new Array(24).fill(0));
    for (const r of this.reservas()) {
      const d = new Date(r.inicio);
      grid[d.getDay()][d.getHours()]++;
    }
    const max = Math.max(1, ...grid.flat());
    return { grid, max };
  });
  heatColor(n: number, max: number): string {
    if (n === 0) return 'var(--c-bg)';
    const t = n / max;
    const nivel = t < 0.25 ? '#bfdbfe' : t < 0.5 ? '#93c5fd' : t < 0.75 ? '#60a5fa' : '#2563eb';
    return nivel;
  }

  // ---- Distribución tiempo de aprobación ----
  distAprobacion = computed(() => {
    const conteo = [0, 0, 0, 0, 0, 0];
    for (const r of this.reservas()) {
      if (!r.resueltaUtc) continue;
      const h = (new Date(r.resueltaUtc).getTime() - new Date(r.creadoUtc).getTime()) / 3_600_000;
      const i = h < 1 ? 0 : h < 4 ? 1 : h < 12 ? 2 : h < 24 ? 3 : h < 48 ? 4 : 5;
      conteo[i]++;
    }
    const max = Math.max(1, ...conteo);
    return this.bucketsAprob.map((label, i) => ({ label, n: conteo[i], pct: (conteo[i] / max) * 100 }));
  });

  // ---- Tabla de rendimiento ----
  rendimiento = computed(() => {
    type Fila = { nombre: string; total: number; apr: number; pen: number; rec: number; ingresos: number; minAprob: number[] };
    const m = new Map<string, Fila>();
    const get = (n: string) => m.get(n) ?? m.set(n, { nombre: n, total: 0, apr: 0, pen: 0, rec: 0, ingresos: 0, minAprob: [] }).get(n)!;
    for (const n of this.amenidadNombres()) get(n);
    for (const r of this.reservas()) {
      const f = get(r.amenidadNombre);
      f.total++;
      if (r.estado === 'Confirmada') { f.apr++; f.ingresos += r.importe ?? 0; }
      else if (r.estado === 'Pendiente') f.pen++;
      else if (r.estado === 'Rechazada') f.rec++;
      if (r.resueltaUtc) f.minAprob.push((new Date(r.resueltaUtc).getTime() - new Date(r.creadoUtc).getTime()) / 60000);
    }
    return [...m.values()].filter((f) => f.total > 0).map((f) => {
      const resueltas = f.apr + f.rec;
      const promMin = f.minAprob.length ? f.minAprob.reduce((a, b) => a + b, 0) / f.minAprob.length : null;
      return {
        nombre: f.nombre, total: f.total, apr: f.apr, pen: f.pen, rec: f.rec,
        pctAprob: resueltas ? Math.round((f.apr / resueltas) * 100) : 0,
        ingresos: f.ingresos,
        tiempoProm: promMin == null ? '—' : promMin < 60 ? `${Math.round(promMin)}m` : `${(promMin / 60).toFixed(1)}h`,
      };
    }).sort((a, b) => b.total - a.total);
  });

  moneda(n: number): string {
    return n.toLocaleString('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
  }

  volver(): void { this.router.navigate(['/panel/amenidades/directorio']); }
}
