import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { AdjuntosComponent } from '../../shared/adjuntos.component';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { TicketService } from '../../core/services/ticket.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_TICKET, CategoriaTicket, EstadoTicket, LABEL_CATEGORIA_TICKET,
  LABEL_ESTADO_TICKET, LABEL_PRIORIDAD, PRIORIDADES_TICKET, PrioridadTicket,
  TicketDetalle, UsuarioAsignable,
} from '../../core/models/ticket.models';

@Component({
  selector: 'app-ticket-detalle',
  standalone: true,
  imports: [FormsModule, DatePipe, AdjuntosComponent],
  templateUrl: './ticket-detalle.component.html',
  styleUrl: './tickets-lista.component.scss',
})
export class TicketDetalleComponent {
  private router = inject(Router);
  private ruta = inject(ActivatedRoute);
  private consorcios = inject(ConsorcioService);
  private api = inject(TicketService);
  private toasts = inject(ToastService);
  private sanitizer = inject(DomSanitizer);

  labelCategoria = LABEL_CATEGORIA_TICKET;
  labelEstado = LABEL_ESTADO_TICKET;
  labelPrioridad = LABEL_PRIORIDAD;
  prioridades = PRIORIDADES_TICKET;

  detalle = signal<TicketDetalle | null>(null);
  cargando = signal(true);
  asignables = signal<UsuarioAsignable[]>([]);
  comentario = signal('');
  enviando = signal(false);
  guardando = signal(false);
  tab = signal<'reporte' | 'conversacion' | 'archivos' | 'acciones'>('reporte');
  convModo = signal<'residente' | 'interna'>('residente');
  accionAbierta = signal<string | null>(null);
  accionMsg = signal('');

  private id = this.ruta.snapshot.paramMap.get('id')!;
  private iconCache = new Map<string, SafeHtml>();

  /** Rótulo humano del estado en el panel derecho. */
  estadoTitulo = computed(() => {
    const e = this.detalle()?.ticket.estado;
    if (e === 'Nuevo') return 'Pendiente de clasificación';
    if (e === 'EnProgreso') return 'En progreso';
    if (e === 'EsperandoInformacion') return 'Esperando información';
    if (e === 'PendienteAprobacion') return 'Pendiente de aprobación';
    if (e === 'Resuelto') return 'Resuelto';
    return '';
  });

  /** Botón de acción principal según el estado actual. */
  accionPrincipal = computed<{ label: string; estado: EstadoTicket; tip: string } | null>(() => {
    const e = this.detalle()?.ticket.estado;
    if (e === 'Nuevo') return { label: '▶ Comenzar trabajo', estado: 'EnProgreso', tip: 'Esto avisa al residente que estás revisando su reporte y cambia el estado a "En progreso".' };
    if (e === 'EnProgreso') return { label: '✓ Marcar resuelto', estado: 'Resuelto', tip: 'Marca el ticket como resuelto y notifica al residente.' };
    if (e === 'EsperandoInformacion' || e === 'PendienteAprobacion') return { label: '▶ Retomar', estado: 'EnProgreso', tip: 'Volver a poner el ticket en progreso.' };
    if (e === 'Resuelto') return { label: '↩ Reabrir', estado: 'EnProgreso', tip: 'Reabre el ticket y lo vuelve a poner en progreso.' };
    return null;
  });

  readonly acciones = computed(() => {
    const e = this.detalle()?.ticket.estado;
    const l: { key: string; label: string; icono: string; estado: EstadoTicket; msg: string; obligatorio: boolean }[] = [];
    if (e === 'Nuevo') {
      l.push({ key: 'comenzar', label: 'Comenzar trabajo', icono: '▶', estado: 'EnProgreso',
        msg: 'Estamos revisando tu reporte y empezamos a trabajarlo.', obligatorio: false });
    }
    if (e === 'Nuevo' || e === 'EnProgreso') {
      l.push({ key: 'info', label: 'Necesito más info', icono: '❔', estado: 'EsperandoInformacion',
        msg: 'Necesitamos información adicional de tu parte para proceder. ¿Podrías darnos más detalles?', obligatorio: false });
    }
    if (e === 'EnProgreso' || e === 'EsperandoInformacion' || e === 'PendienteAprobacion') {
      l.push({ key: 'resolver', label: 'Marcar resuelto', icono: '✓', estado: 'Resuelto',
        msg: 'Tu reporte fue resuelto. Si tenés alguna otra inquietud, escribinos.', obligatorio: true });
    }
    if (e === 'Resuelto') {
      l.push({ key: 'reabrir', label: 'Reabrir ticket', icono: '↩', estado: 'EnProgreso', msg: '', obligatorio: false });
    }
    return l;
  });

  abrirAccion(a: { key: string; msg: string }): void {
    if (this.accionAbierta() === a.key) { this.accionAbierta.set(null); return; }
    this.accionAbierta.set(a.key);
    this.accionMsg.set(a.msg);
  }

  ejecutarAccion(a: { estado: EstadoTicket; obligatorio: boolean }): void {
    const cid = this.consorcios.activoId();
    if (!cid) return;
    const msg = this.accionMsg().trim();
    if (a.obligatorio && !msg) { this.toasts.error('Esta acción requiere una nota.'); return; }
    const aplicar = () => {
      this.actualizar({ estado: a.estado });
      this.accionAbierta.set(null);
      this.accionMsg.set('');
      this.toasts.exito('Estado actualizado');
    };
    if (msg) {
      this.api.comentar(cid, this.id, msg, false).subscribe({ next: aplicar, error: aplicar });
    } else {
      aplicar();
    }
  }

  constructor() {
    const cid = this.consorcios.activoId();
    if (cid) {
      this.api.obtener(cid, this.id).subscribe({
        next: (d) => { this.detalle.set(d); this.cargando.set(false); },
        error: () => { this.toasts.error('No se pudo cargar el ticket.'); this.cargando.set(false); this.volver(); },
      });
      this.api.asignables(cid).subscribe((a) => this.asignables.set(a));
    }
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

  private recargar(): void {
    const cid = this.consorcios.activoId();
    if (cid) this.api.obtener(cid, this.id).subscribe((d) => this.detalle.set(d));
  }

  private actualizar(cambios: Partial<{ estado: EstadoTicket; prioridad: PrioridadTicket; asignadoAUsuarioId: string | null }>): void {
    const cid = this.consorcios.activoId();
    const d = this.detalle();
    if (!cid || !d) return;
    this.guardando.set(true);
    this.api.actualizar(cid, this.id, {
      estado: cambios.estado ?? d.ticket.estado,
      prioridad: cambios.prioridad ?? d.ticket.prioridad,
      asignadoAUsuarioId: cambios.asignadoAUsuarioId ?? null,
    }).subscribe({
      next: () => { this.guardando.set(false); this.recargar(); this.api.refrescarActivos(cid); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo actualizar.'); },
    });
  }

  cambiarPrioridad(p: string): void { this.actualizar({ prioridad: p as PrioridadTicket }); }
  asignar(usuarioId: string): void {
    this.actualizar({ asignadoAUsuarioId: usuarioId || null });
    this.toasts.exito(usuarioId ? 'Ticket asignado' : 'Asignación quitada');
  }
  hacerAccion(): void {
    const a = this.accionPrincipal();
    if (a) { this.actualizar({ estado: a.estado }); this.toasts.exito('Estado actualizado'); }
  }

  comentar(): void {
    const cid = this.consorcios.activoId();
    const txt = this.comentario().trim();
    if (!cid || !txt || this.enviando()) return;
    this.enviando.set(true);
    this.api.comentar(cid, this.id, txt, this.convModo() === 'interna').subscribe({
      next: () => { this.enviando.set(false); this.comentario.set(''); this.recargar(); },
      error: (e) => { this.enviando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo comentar.'); },
    });
  }

  comentariosFiltrados = computed(() => {
    const interna = this.convModo() === 'interna';
    return (this.detalle()?.comentarios ?? []).filter((c) => c.esInterna === interna);
  });

  archivar(): void {
    const cid = this.consorcios.activoId();
    const d = this.detalle();
    if (!cid || !d) return;
    this.api.archivar(cid, this.id, !d.ticket.archivado).subscribe({
      next: () => {
        this.toasts.exito(d.ticket.archivado ? 'Ticket reabierto' : 'Ticket archivado');
        this.api.refrescarActivos(cid);
        this.volver();
      },
      error: () => this.toasts.error('No se pudo archivar.'),
    });
  }

  volver(): void { this.router.navigate(['/panel/tickets/lista']); }
}
