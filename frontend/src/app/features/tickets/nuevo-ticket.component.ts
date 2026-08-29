import { Component, computed, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { forkJoin, of } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { TicketService } from '../../core/services/ticket.service';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { ToastService } from '../../core/services/toast.service';
import { Unidad } from '../../core/models/consorcio.models';
import {
  CATEGORIAS_TICKET, CategoriaTicket, PRIORIDADES_TICKET, PrioridadTicket, UsuarioAsignable,
} from '../../core/models/ticket.models';

@Component({
  selector: 'app-nuevo-ticket',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './nuevo-ticket.component.html',
  styleUrl: './tickets-lista.component.scss',
})
export class NuevoTicketComponent {
  private consorcios = inject(ConsorcioService);
  private unidadesApi = inject(UnidadService);
  private api = inject(TicketService);
  private adjuntosApi = inject(AdjuntoService);
  private toasts = inject(ToastService);
  private sanitizer = inject(DomSanitizer);

  cerrar = output<void>();
  creado = output<void>();

  categorias = CATEGORIAS_TICKET;
  prioridades = PRIORIDADES_TICKET;

  categoria = signal<CategoriaTicket>('Mantenimiento');
  prioridad = signal<PrioridadTicket>('Media');
  asignadoA = signal<string>('');
  fechaLimite = signal<string>('');
  descripcion = signal<string>('');
  etiquetaBorrador = signal<string>('');
  etiquetas = signal<string[]>([]);
  ubicacion = signal<string>('');
  archivos = signal<File[]>([]);
  enviando = signal(false);

  unidadId = signal<string>('');
  unidades = signal<Unidad[]>([]);
  asignables = signal<UsuarioAsignable[]>([]);

  private iconCache = new Map<string, SafeHtml>();

  puedeEnviar = computed(() => this.descripcion().trim().length > 0 && !this.enviando());

  constructor() {
    const cid = this.consorcios.activoId();
    if (cid) {
      this.unidadesApi.listar(cid).subscribe((u) => this.unidades.set(u));
      this.api.asignables(cid).subscribe((a) => this.asignables.set(a));
    }
  }

  icono(cat: CategoriaTicket): SafeHtml {
    if (!this.iconCache.has(cat)) {
      const path = this.categorias.find((c) => c.value === cat)!.icon;
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6"
        stroke-linecap="round" stroke-linejoin="round">${path}</svg>`;
      this.iconCache.set(cat, this.sanitizer.bypassSecurityTrustHtml(svg));
    }
    return this.iconCache.get(cat)!;
  }

  agregarEtiqueta(): void {
    const t = this.etiquetaBorrador().trim();
    if (t && !this.etiquetas().includes(t)) this.etiquetas.update((e) => [...e, t]);
    this.etiquetaBorrador.set('');
  }

  quitarEtiqueta(t: string): void {
    this.etiquetas.update((e) => e.filter((x) => x !== t));
  }

  onArchivos(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (!input.files) return;
    this.sumarArchivos(Array.from(input.files));
    input.value = '';
  }

  onDrop(ev: DragEvent): void {
    ev.preventDefault();
    if (ev.dataTransfer?.files) this.sumarArchivos(Array.from(ev.dataTransfer.files));
  }

  private sumarArchivos(files: File[]): void {
    const libres = 7 - this.archivos().length;
    if (libres <= 0) { this.toasts.error('Máximo 7 archivos.'); return; }
    this.archivos.update((a) => [...a, ...files.slice(0, libres)]);
  }

  quitarArchivo(f: File): void {
    this.archivos.update((a) => a.filter((x) => x !== f));
  }

  esImagen(f: File): boolean {
    return f.type.startsWith('image/');
  }

  vistaPrevia(f: File): string {
    return URL.createObjectURL(f);
  }

  enviar(): void {
    const cid = this.consorcios.activoId();
    if (!cid || !this.puedeEnviar()) return;
    this.enviando.set(true);

    this.api.crear(cid, {
      descripcion: this.descripcion().trim(),
      categoria: this.categoria(),
      prioridad: this.prioridad(),
      unidadId: this.unidadId() || null,
      asignadoAUsuarioId: this.asignadoA() || null,
      fechaLimite: this.fechaLimite() || null,
      etiquetas: this.etiquetas().length ? this.etiquetas() : null,
      ubicacion: this.ubicacion().trim() || null,
    }).subscribe({
      next: (ticket) => {
        const subidas = this.archivos().map((f) => this.adjuntosApi.subir('Ticket', ticket.id, f));
        (subidas.length ? forkJoin(subidas) : of([])).subscribe({
          next: () => this.terminar(),
          error: () => {
            this.toasts.info('Ticket creado, pero algún archivo no se pudo adjuntar.');
            this.terminar();
          },
        });
      },
      error: (e) => {
        this.enviando.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo crear el ticket.');
      },
    });
  }

  private terminar(): void {
    this.enviando.set(false);
    this.toasts.exito('Ticket creado');
    this.creado.emit();
  }
}
