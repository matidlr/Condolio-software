import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { TicketService } from '../../core/services/ticket.service';
import {
  LABEL_CATEGORIA_TICKET, LABEL_ESTADO_TICKET, Ticket,
} from '../../core/models/ticket.models';

@Component({
  selector: 'app-tickets-panel',
  standalone: true,
  templateUrl: './tickets-panel.component.html',
  styleUrl: './tickets-panel.component.scss',
})
export class TicketsPanelComponent {
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(TicketService);

  labelEstado = LABEL_ESTADO_TICKET;
  labelCategoria = LABEL_CATEGORIA_TICKET;

  cargando = signal(true);
  tickets = signal<Ticket[]>([]);

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

  abiertos = computed(() => this.tickets().filter((t) => !t.archivado && t.estado !== 'Resuelto'));

  totalAbiertos = computed(() => this.abiertos().length);
  pendientes = computed(() => this.abiertos().filter((t) => t.estado === 'Nuevo').length);
  enProgreso = computed(() => this.abiertos().filter((t) => t.estado === 'EnProgreso').length);
  enProgresoAsignados = computed(() =>
    this.abiertos().filter((t) => t.estado === 'EnProgreso' && t.asignadoA).length);

  requierenAtencion = computed(() =>
    this.abiertos().filter((t) => this.horas(t.estadoDesdeUtc) >= 48
      || (t.prioridad === 'Critica' && !t.asignadoA)).length);

  antiguedad = computed(() => {
    const a = this.abiertos();
    const b = { m24: 0, e2448: 0, p48: 0 };
    for (const t of a) {
      const h = this.horas(t.reportadoUtc);
      if (h < 24) b.m24++;
      else if (h < 48) b.e2448++;
      else b.p48++;
    }
    const total = a.length || 1;
    return [
      { label: '< 24 horas', n: b.m24, pct: Math.round((b.m24 / total) * 100) },
      { label: '24-48 horas', n: b.e2448, pct: Math.round((b.e2448 / total) * 100) },
      { label: '> 48 horas', n: b.p48, pct: Math.round((b.p48 / total) * 100) },
    ];
  });

  cargaPersonal = computed(() => {
    const a = this.abiertos();
    const porPersona = new Map<string, { asignados: number; nuevos: number }>();
    let sinAsignar = 0;
    let sinAsignarNuevos = 0;
    for (const t of a) {
      if (!t.asignadoA) {
        sinAsignar++;
        if (t.estado === 'Nuevo') sinAsignarNuevos++;
        continue;
      }
      const e = porPersona.get(t.asignadoA) ?? { asignados: 0, nuevos: 0 };
      e.asignados++;
      if (t.estado === 'Nuevo') e.nuevos++;
      porPersona.set(t.asignadoA, e);
    }
    const personas = [...porPersona.entries()].map(([nombre, v]) => ({ nombre, ...v }));
    const prom = personas.length ? Math.round(a.length / personas.length) : 0;
    return { sinAsignar, sinAsignarNuevos, personas, prom, totalPersonal: personas.length };
  });

  alertas = computed(() => {
    const out: string[] = [];
    const criticos = this.abiertos().filter((t) => t.prioridad === 'Critica' && !t.asignadoA);
    if (criticos.length) out.push(`${criticos.length} ticket(s) crítico(s) sin asignar`);
    const viejos = this.abiertos().filter((t) => this.horas(t.reportadoUtc) >= 72);
    if (viejos.length) out.push(`${viejos.length} ticket(s) abierto(s) hace más de 3 días`);
    const sa = this.cargaPersonal().sinAsignar;
    if (sa >= 5) out.push(`${sa} tickets esperando asignación`);
    return out;
  });

  private horas(iso: string): number {
    return (Date.now() - new Date(iso).getTime()) / 3_600_000;
  }

  irAnalisis(): void { this.router.navigate(['/panel/tickets/metricas']); }
  irLista(filtro?: string): void {
    this.router.navigate(['/panel/tickets/lista'], filtro ? { queryParams: { rapido: filtro } } : {});
  }
  volver(): void { this.router.navigate(['/panel/tickets/lista']); }
}
