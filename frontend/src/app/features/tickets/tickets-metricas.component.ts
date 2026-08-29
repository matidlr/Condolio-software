import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { TicketService } from '../../core/services/ticket.service';
import { LABEL_CATEGORIA_TICKET, Ticket } from '../../core/models/ticket.models';

type Periodo = 'semana' | 'mes' | 'trimestre' | 'anio';

interface PuntoDia {
  fecha: string;      // yyyy-mm-dd
  label: string;      // "Aug 10"
  x: number;
  creados: number;
  resueltos: number;
  backlog: number;
  cyCre: number;
  cyBack: number;
}

@Component({
  selector: 'app-tickets-metricas',
  standalone: true,
  templateUrl: './tickets-metricas.component.html',
  styleUrl: './tickets-panel.component.scss',
})
export class TicketsMetricasComponent {
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(TicketService);

  labelCategoria = LABEL_CATEGORIA_TICKET;

  cargando = signal(true);
  tickets = signal<Ticket[]>([]);
  periodo = signal<Periodo>('mes');
  hover = signal<number | null>(null);

  readonly periodos: { k: Periodo; l: string }[] = [
    { k: 'semana', l: 'Semana' }, { k: 'mes', l: 'Mes' },
    { k: 'trimestre', l: 'Trimestre' }, { k: 'anio', l: 'Año' },
  ];

  readonly W = 520;
  readonly H = 200;
  readonly PAD = 28;

  private consorcioId = computed(() => this.consorcios.activoId());

  constructor() {
    effect(() => {
      const id = this.consorcioId();
      if (!id) return;
      this.cargando.set(true);
      this.api.todos(id).subscribe({
        next: (t) => { this.tickets.set(t); this.cargando.set(false); },
        error: () => this.cargando.set(false),
      });
    });
  }

  private dias(): number {
    return { semana: 7, mes: 30, trimestre: 90, anio: 365 }[this.periodo()];
  }

  private desde(): Date {
    const d = new Date();
    d.setHours(0, 0, 0, 0);
    d.setDate(d.getDate() - (this.dias() - 1));
    return d;
  }

  enPeriodo = computed(() => {
    const desde = this.desde().getTime();
    return this.tickets().filter((t) => new Date(t.reportadoUtc).getTime() >= desde);
  });

  totalReportes = computed(() => this.enPeriodo().length);

  resueltosPeriodo = computed(() =>
    this.enPeriodo().filter((t) => t.estado === 'Resuelto' || t.archivado));

  tasaResolucion = computed(() => {
    const tot = this.totalReportes();
    return tot ? Math.round((this.resueltosPeriodo().length / tot) * 100) : 0;
  });

  abiertosPeriodo = computed(() =>
    this.enPeriodo().filter((t) => t.estado !== 'Resuelto' && !t.archivado).length);

  tiempoPromedio = computed(() => {
    const res = this.resueltosPeriodo();
    if (!res.length) return null;
    const horas = res.reduce((acc, t) =>
      acc + (new Date(t.ultimaActividadUtc).getTime() - new Date(t.reportadoUtc).getTime()) / 3_600_000, 0) / res.length;
    if (horas < 48) return `${Math.round(horas)} h`;
    return `${(horas / 24).toFixed(1)} d`;
  });

  // Agrupación: por día si <= 45 días, si no por semana
  private cubos(): { fecha: Date; label: string }[] {
    const total = this.dias();
    const paso = total <= 45 ? 1 : 7;
    const out: { fecha: Date; label: string }[] = [];
    const base = this.desde();
    for (let i = 0; i < total; i += paso) {
      const f = new Date(base);
      f.setDate(base.getDate() + i);
      out.push({ fecha: f, label: f.toLocaleDateString('es-AR', { day: '2-digit', month: 'short' }) });
    }
    return out;
  }

  serie = computed<PuntoDia[]>(() => {
    const cubos = this.cubos();
    const pasoMs = (cubos.length > 1
      ? cubos[1].fecha.getTime() - cubos[0].fecha.getTime()
      : 86_400_000);
    const tk = this.tickets();

    const crePorCubo = new Array(cubos.length).fill(0);
    const resPorCubo = new Array(cubos.length).fill(0);
    const idx = (ms: number) => Math.floor((ms - cubos[0].fecha.getTime()) / pasoMs);

    for (const t of tk) {
      const ci = idx(new Date(t.reportadoUtc).getTime());
      if (ci >= 0 && ci < cubos.length) crePorCubo[ci]++;
      if (t.estado === 'Resuelto' || t.archivado) {
        const ri = idx(new Date(t.ultimaActividadUtc).getTime());
        if (ri >= 0 && ri < cubos.length) resPorCubo[ri]++;
      }
    }

    const maxV = Math.max(1, ...crePorCubo, ...resPorCubo,
      ...crePorCubo.map((_, i) => Math.abs(crePorCubo.slice(0, i + 1).reduce((a, b) => a + b, 0)
        - resPorCubo.slice(0, i + 1).reduce((a, b) => a + b, 0))));

    const innerW = this.W - this.PAD * 2;
    const innerH = this.H - this.PAD * 2;
    let acum = 0;

    return cubos.map((c, i) => {
      acum += crePorCubo[i] - resPorCubo[i];
      const x = this.PAD + (cubos.length === 1 ? innerW / 2 : (innerW * i) / (cubos.length - 1));
      return {
        fecha: c.fecha.toISOString().slice(0, 10),
        label: c.label,
        x,
        creados: crePorCubo[i],
        resueltos: resPorCubo[i],
        backlog: acum,
        cyCre: this.PAD + innerH - (crePorCubo[i] / maxV) * innerH,
        cyBack: this.PAD + innerH - (acum / maxV) * innerH,
      };
    });
  });

  cambioNeto = computed(() => {
    const s = this.serie();
    return s.length ? s[s.length - 1].backlog : 0;
  });

  ejeY = computed(() => {
    const s = this.serie();
    const max = Math.max(1, ...s.map((p) => Math.max(p.creados, p.resueltos, p.backlog)));
    const paso = Math.ceil(max / 4) || 1;
    return [0, 1, 2, 3, 4].map((i) => {
      const v = i * paso;
      const innerH = this.H - this.PAD * 2;
      return { v, y: this.PAD + innerH - (v / (paso * 4)) * innerH };
    });
  });

  lineaBacklog = computed(() =>
    this.serie().map((p, i) => `${i === 0 ? 'M' : 'L'} ${p.x} ${p.cyBack}`).join(' '));

  distribucionCategoria = computed(() => {
    const enP = this.enPeriodo();
    const total = enP.length || 1;
    const conteo = new Map<string, number>();
    for (const t of enP) conteo.set(t.categoria, (conteo.get(t.categoria) ?? 0) + 1);
    return [...conteo.entries()]
      .map(([cat, n]) => ({
        label: this.labelCategoria[cat as keyof typeof LABEL_CATEGORIA_TICKET],
        n, pct: Math.round((n / total) * 100),
      }))
      .sort((a, b) => b.n - a.n);
  });

  irPanel(): void { this.router.navigate(['/panel/tickets/panel']); }
  volver(): void { this.router.navigate(['/panel/tickets/lista']); }
}
