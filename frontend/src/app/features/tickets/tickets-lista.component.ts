import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { forkJoin } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { TicketService } from '../../core/services/ticket.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_TICKET, CategoriaTicket, ESTADOS_TICKET, EstadoTicket,
  LABEL_CATEGORIA_TICKET, LABEL_ESTADO_TICKET, LABEL_PRIORIDAD,
  PRIORIDADES_TICKET, PrioridadTicket, Ticket, UsuarioAsignable,
} from '../../core/models/ticket.models';
import { NuevoTicketComponent } from './nuevo-ticket.component';

type Vista = 'activos' | 'archivo';

@Component({
  selector: 'app-tickets-lista',
  standalone: true,
  imports: [FormsModule, NuevoTicketComponent],
  templateUrl: './tickets-lista.component.html',
  styleUrl: './tickets-lista.component.scss',
})
export class TicketsListaComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(TicketService);
  private toasts = inject(ToastService);
  private sanitizer = inject(DomSanitizer);

  labelCategoria = LABEL_CATEGORIA_TICKET;
  labelEstado = LABEL_ESTADO_TICKET;
  labelPrioridad = LABEL_PRIORIDAD;
  estados = ESTADOS_TICKET;
  prioridades = PRIORIDADES_TICKET;

  tickets = signal<Ticket[]>([]);
  activos = signal(0);
  archivados = signal(0);
  cargando = signal(true);
  vista = signal<Vista>('activos');
  busqueda = signal('');
  nuevoAbierto = signal(false);

  seleccion = signal<Set<string>>(new Set());
  asignables = signal<UsuarioAsignable[]>([]);

  // modales de acción masiva
  modal = signal<'estado' | 'asignar' | 'prioridad' | 'eliminar' | null>(null);
  nuevoEstado = signal<EstadoTicket>('EnProgreso');
  nuevaPrioridad = signal<PrioridadTicket>('Media');
  nuevoAsignado = signal<string>('');
  aplicando = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return this.tickets().filter((t) => !q
      || (t.descripcion ?? '').toLowerCase().includes(q)
      || (t.titulo ?? '').toLowerCase().includes(q)
      || (t.unidadNombre ?? '').toLowerCase().includes(q)
      || this.labelCategoria[t.categoria].toLowerCase().includes(q));
  });

  todasSel = computed(() => {
    const v = this.visibles();
    return v.length > 0 && v.every((t) => this.seleccion().has(t.id));
  });

  private iconCache = new Map<string, SafeHtml>();

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
    effect(() => { this.vista(); const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.seleccion.set(new Set());
    this.api.listar(cid, this.vista() === 'archivo').subscribe({
      next: (l) => {
        this.tickets.set(l.tickets);
        this.activos.set(l.activos);
        this.archivados.set(l.archivados);
        this.api.activos.set(l.activos);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar los tickets.'); this.cargando.set(false); },
    });
    this.api.asignables(cid).subscribe((a) => this.asignables.set(a));
  }

  recargar(): void {
    const id = this.consorcioId();
    if (id) this.cargar(id);
  }

  onCreado(): void {
    this.nuevoAbierto.set(false);
    this.recargar();
  }

  icono(cat: CategoriaTicket): SafeHtml {
    if (!this.iconCache.has(cat)) {
      const path = CATEGORIAS_TICKET.find((c) => c.value === cat)?.icon ?? '';
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"
        stroke-linecap="round" stroke-linejoin="round">${path}</svg>`;
      this.iconCache.set(cat, this.sanitizer.bypassSecurityTrustHtml(svg));
    }
    return this.iconCache.get(cat)!;
  }

  // ---- selección ----
  toggleUno(id: string): void {
    const s = new Set(this.seleccion());
    s.has(id) ? s.delete(id) : s.add(id);
    this.seleccion.set(s);
  }

  toggleTodas(): void {
    if (this.todasSel()) { this.seleccion.set(new Set()); return; }
    this.seleccion.set(new Set(this.visibles().map((t) => t.id)));
  }

  limpiarSel(): void {
    this.seleccion.set(new Set());
  }

  // ---- fechas / duración ----
  fechaCorta(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: '2-digit', month: 'short' })
      + ' ' + new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  desde(iso: string): string {
    const ms = Date.now() - new Date(iso).getTime();
    const h = Math.floor(ms / 3_600_000);
    if (h < 1) return `${Math.max(1, Math.floor(ms / 60_000))}m`;
    if (h < 24) return `${h}h`;
    const d = Math.floor(h / 24);
    const rh = h % 24;
    return rh ? `${d}d ${rh}h` : `${d}d`;
  }

  vencido(t: Ticket): boolean {
    const dias = (Date.now() - new Date(t.estadoDesdeUtc).getTime()) / 86_400_000;
    return dias >= 1;
  }

  // ---- acciones masivas ----
  abrirModal(m: 'estado' | 'asignar' | 'prioridad' | 'eliminar'): void {
    if (!this.seleccion().size) return;
    this.modal.set(m);
  }

  private idsSel(): string[] {
    return [...this.seleccion()];
  }

  aplicarEstado(): void {
    this.aplicarMasivo((cid, id, t) =>
      this.api.actualizar(cid, id, { estado: this.nuevoEstado(), prioridad: t.prioridad, asignadoAUsuarioId: null }),
      'Estado actualizado');
  }

  aplicarPrioridad(): void {
    this.aplicarMasivo((cid, id, t) =>
      this.api.actualizar(cid, id, { estado: t.estado, prioridad: this.nuevaPrioridad(), asignadoAUsuarioId: null }),
      'Prioridad actualizada');
  }

  aplicarAsignacion(): void {
    this.aplicarMasivo((cid, id, t) =>
      this.api.actualizar(cid, id, { estado: t.estado, prioridad: t.prioridad, asignadoAUsuarioId: this.nuevoAsignado() || null }),
      'Tickets asignados');
  }

  aplicarEliminar(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    const n = this.seleccion().size;
    this.aplicando.set(true);
    forkJoin(this.idsSel().map((id) => this.api.eliminar(cid, id))).subscribe({
      next: () => this.finMasivo(n === 1 ? 'Reporte eliminado' : 'Reportes eliminados'),
      error: () => { this.aplicando.set(false); this.toasts.error('No se pudieron eliminar todos.'); },
    });
  }

  private aplicarMasivo(
    fn: (cid: string, id: string, t: Ticket) => ReturnType<TicketService['actualizar']>,
    ok: string,
  ): void {
    const cid = this.consorcioId();
    if (!cid) return;
    const mapa = new Map(this.tickets().map((t) => [t.id, t]));
    this.aplicando.set(true);
    forkJoin(this.idsSel().map((id) => fn(cid, id, mapa.get(id)!))).subscribe({
      next: () => this.finMasivo(ok),
      error: () => { this.aplicando.set(false); this.toasts.error('No se pudo aplicar a todos.'); },
    });
  }

  private finMasivo(msg: string): void {
    this.aplicando.set(false);
    this.modal.set(null);
    this.toasts.exito(msg);
    this.recargar();
  }
}
