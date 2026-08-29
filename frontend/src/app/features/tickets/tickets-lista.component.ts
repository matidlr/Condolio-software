import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
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
type FiltroDim = 'estado' | 'categoria' | 'prioridad' | 'asignado' | 'fechas';

interface FiltrosTicket {
  estado: Set<EstadoTicket>;
  categoria: Set<CategoriaTicket>;
  prioridad: Set<PrioridadTicket>;
  asignado: Set<string>; // nombre, o '' para "Por asignar"
  desde: string;
  hasta: string;
}

const SIN_FILTROS = (): FiltrosTicket => ({
  estado: new Set(), categoria: new Set(), prioridad: new Set(),
  asignado: new Set(), desde: '', hasta: '',
});

@Component({
  selector: 'app-tickets-lista',
  standalone: true,
  imports: [FormsModule, NuevoTicketComponent],
  templateUrl: './tickets-lista.component.html',
  styleUrl: './tickets-lista.component.scss',
})
export class TicketsListaComponent {
  private auth = inject(AuthService);
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

  categorias = CATEGORIAS_TICKET;

  // ---- filtros ----
  filtroAbierto = signal(false);
  filtroDim = signal<FiltroDim>('estado');
  filtros = signal<FiltrosTicket>(SIN_FILTROS());

  // ---- columnas ----
  columnasAbierto = signal(false);
  readonly columnasDef: { key: string; label: string }[] = [
    { key: 'categoria', label: 'Categoría' },
    { key: 'estado', label: 'Estado' },
    { key: 'prioridad', label: 'Prioridad' },
    { key: 'descripcion', label: 'Descripción' },
    { key: 'reportadoPor', label: 'Reportado por' },
    { key: 'fecha', label: 'Fecha' },
    { key: 'tiempoEstado', label: 'Tiempo en estado' },
    { key: 'diasAbierto', label: 'Días abierto' },
    { key: 'asignado', label: 'Asignado a' },
    { key: 'unidad', label: 'Unidad' },
    { key: 'ultimaActividad', label: 'Última actividad' },
  ];
  columnasVisibles = signal<Set<string>>(new Set(this.columnasDef.map((c) => c.key)));

  verCol(key: string): boolean {
    return this.columnasVisibles().has(key);
  }

  toggleColumna(key: string): void {
    const s = new Set(this.columnasVisibles());
    s.has(key) ? s.delete(key) : s.add(key);
    if (s.size === 0) return; // no permitir cero columnas
    this.columnasVisibles.set(s);
  }

  rapidosAbierto = signal(false);
  rapidoActivo = signal<string | null>(null);
  readonly rapidos = [
    { k: 'mis', l: 'Mis tickets', ic: '👤' },
    { k: 'sinasignar', l: 'Sin asignar', ic: '👥' },
    { k: 'urgente', l: 'Urgente', ic: '⚠' },
    { k: 'triaje', l: 'Pendiente de triaje', ic: '⚑' },
    { k: 'progreso', l: 'En progreso', ic: '▶' },
  ];
  rapidoLabel = computed(() => {
    const k = this.rapidoActivo();
    return k ? this.rapidos.find((r) => r.k === k)?.l ?? 'Filtros rápidos' : 'Filtros rápidos';
  });

  filtrosActivos = computed(() => {
    const f = this.filtros();
    return f.estado.size + f.categoria.size + f.prioridad.size + f.asignado.size
      + (f.desde ? 1 : 0) + (f.hasta ? 1 : 0);
  });

  /** Nombres de asignados presentes en la tanda actual (para el submenú "Asignado a"). */
  asignadosDisponibles = computed(() => {
    const set = new Set<string>();
    for (const t of this.tickets()) set.add(t.asignadoA ?? '');
    return [...set].sort((a, b) => (a === '' ? -1 : b === '' ? 1 : a.localeCompare(b)));
  });

  contar(dim: FiltroDim, valor: string): number {
    return this.tickets().filter((t) => {
      switch (dim) {
        case 'estado': return t.estado === valor;
        case 'categoria': return t.categoria === valor;
        case 'prioridad': return t.prioridad === valor;
        case 'asignado': return (t.asignadoA ?? '') === valor;
        default: return false;
      }
    }).length;
  }

  // modales de acción masiva
  modal = signal<'estado' | 'asignar' | 'prioridad' | 'eliminar' | null>(null);
  nuevoEstado = signal<EstadoTicket>('EnProgreso');
  nuevaPrioridad = signal<PrioridadTicket>('Media');
  nuevoAsignado = signal<string>('');
  aplicando = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const f = this.filtros();
    return this.tickets().filter((t) => {
      if (q && !(
        (t.descripcion ?? '').toLowerCase().includes(q)
        || (t.titulo ?? '').toLowerCase().includes(q)
        || (t.unidadNombre ?? '').toLowerCase().includes(q)
        || this.labelCategoria[t.categoria].toLowerCase().includes(q))) return false;
      if (f.estado.size && !f.estado.has(t.estado)) return false;
      if (f.categoria.size && !f.categoria.has(t.categoria)) return false;
      if (f.prioridad.size && !f.prioridad.has(t.prioridad)) return false;
      if (f.asignado.size && !f.asignado.has(t.asignadoA ?? '')) return false;
      if (f.desde && t.reportadoUtc.slice(0, 10) < f.desde) return false;
      if (f.hasta && t.reportadoUtc.slice(0, 10) > f.hasta) return false;
      return true;
    });
  });

  todasSel = computed(() => {
    const v = this.visibles();
    return v.length > 0 && v.every((t) => this.seleccion().has(t.id));
  });

  private iconCache = new Map<string, SafeHtml>();

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
    effect(() => { this.vista(); const id = this.consorcioId(); if (id) this.cargar(id); });

    const rapido = inject(ActivatedRoute).snapshot.queryParamMap.get('rapido');
    if (rapido && this.rapidos.some((r) => r.k === rapido)) this.aplicarRapido(rapido);
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

  // ---- filtros ----
  private clonar(f: FiltrosTicket): FiltrosTicket {
    return {
      estado: new Set(f.estado), categoria: new Set(f.categoria),
      prioridad: new Set(f.prioridad), asignado: new Set(f.asignado),
      desde: f.desde, hasta: f.hasta,
    };
  }

  private setDe(f: FiltrosTicket, dim: FiltroDim): Set<string> {
    return f[dim as 'estado' | 'categoria' | 'prioridad' | 'asignado'] as Set<string>;
  }

  filtroTiene(dim: FiltroDim, valor: string): boolean {
    return this.setDe(this.filtros(), dim).has(valor);
  }

  toggleFiltro(dim: FiltroDim, valor: string): void {
    this.filtros.update((f) => {
      const c = this.clonar(f);
      const s = this.setDe(c, dim);
      s.has(valor) ? s.delete(valor) : s.add(valor);
      return c;
    });
  }

  seleccionarTodosFiltro(dim: FiltroDim, valores: string[]): void {
    this.filtros.update((f) => {
      const c = this.clonar(f);
      const s = this.setDe(c, dim);
      s.clear();
      valores.forEach((v) => s.add(v));
      return c;
    });
  }

  limpiarDim(dim: FiltroDim): void {
    this.filtros.update((f) => {
      const c = this.clonar(f);
      if (dim === 'fechas') { c.desde = ''; c.hasta = ''; }
      else this.setDe(c, dim).clear();
      return c;
    });
  }

  setFecha(cual: 'desde' | 'hasta', valor: string): void {
    this.filtros.update((f) => ({ ...this.clonar(f), [cual]: valor }));
  }

  rangoPreset(dias: number): void {
    const hoy = new Date();
    const iso = (d: Date) => d.toISOString().slice(0, 10);
    const desde = new Date(hoy);
    desde.setDate(hoy.getDate() - (dias - 1));
    this.filtros.update((f) => ({ ...this.clonar(f), desde: iso(desde), hasta: iso(hoy) }));
  }

  limpiarFiltros(): void {
    this.filtros.set(SIN_FILTROS());
    this.rapidoActivo.set(null);
  }

  aplicarRapido(k: string): void {
    this.rapidosAbierto.set(false);
    if (this.rapidoActivo() === k) { this.limpiarFiltros(); return; }
    const f = SIN_FILTROS();
    switch (k) {
      case 'mis': f.asignado = new Set([this.auth.nombre()]); break;
      case 'sinasignar': f.asignado = new Set(['']); break;
      case 'urgente': f.prioridad = new Set<PrioridadTicket>(['Critica', 'Alta']); break;
      case 'triaje': f.estado = new Set<EstadoTicket>(['Nuevo']); break;
      case 'progreso': f.estado = new Set<EstadoTicket>(['EnProgreso']); break;
    }
    this.filtros.set(f);
    this.rapidoActivo.set(k);
  }

  estadosValores(): string[] { return this.estados.map((e) => e.value); }
  categoriasValores(): string[] { return this.categorias.map((c) => c.value); }
  prioridadesValores(): string[] { return this.prioridades.map((p) => p.value); }

  // ---- exportar ----
  exportarAbierto = signal(false);
  formatoExport = signal<'csv' | 'xlsx' | 'pdf'>('xlsx');

  exportar(): void {
    const rows = this.visibles();
    const headers = ['#', 'Categoría', 'Estado', 'Prioridad', 'Título', 'Descripción',
      'Reportado por', 'Reportado', 'Asignado a', 'Unidad', 'Etiquetas', 'Ubicación',
      'Vence', 'Última actividad'];
    const linea = (v: string) => `"${(v ?? '').replace(/"/g, '""')}"`;
    const cuerpo = rows.map((t) => [
      t.numero, this.labelCategoria[t.categoria], this.labelEstado[t.estado], this.labelPrioridad[t.prioridad],
      t.titulo ?? '', t.descripcion, t.reportadoPor, t.reportadoUtc.slice(0, 16).replace('T', ' '),
      t.asignadoA ?? 'Por asignar', t.unidadNombre ?? '', t.etiquetas.join('; '), t.ubicacion ?? '',
      t.fechaLimite ? t.fechaLimite.slice(0, 10) : '', t.ultimaActividadUtc.slice(0, 16).replace('T', ' '),
    ].map((c) => linea(String(c))).join(','));
    const csv = '﻿' + [headers.map(linea).join(','), ...cuerpo].join('\r\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `tickets-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);

    this.exportarAbierto.set(false);
    if (this.formatoExport() !== 'csv') {
      this.toasts.info('Por ahora la exportación se genera en CSV (se abre en Excel).');
    } else {
      this.toasts.exito(`${rows.length} ticket(s) exportado(s)`);
    }
  }

  diasAbierto(iso: string): number {
    return Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000));
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
