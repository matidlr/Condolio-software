import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CATEGORIAS_INCIDENCIA, Incidencia, IncidenciaDetalle, META_ESTADO_INC, MiIncidenciaService,
} from '../../core/services/mi-incidencia.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-incidencias',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-incidencias.component.html',
  styleUrl: './portal-incidencias.component.scss',
})
export class PortalIncidenciasComponent {
  private api = inject(MiIncidenciaService);
  private http = inject(HttpClient);
  private toasts = inject(ToastService);

  categorias = CATEGORIAS_INCIDENCIA;
  metaEstado = META_ESTADO_INC;

  vista = signal<'lista' | 'nuevo' | 'detalle'>('lista');
  cargando = signal(true);
  incidencias = signal<Incidencia[]>([]);
  tab = signal<'activo' | 'resuelto'>('activo');

  // nuevo
  fCategoria = signal('Mantenimiento');
  fDescripcion = signal('');
  fArchivos = signal<{ file: File; url: string }[]>([]);
  enviando = signal(false);

  // detalle
  detalle = signal<IncidenciaDetalle | null>(null);
  imgUrls = signal<Record<string, string>>({});
  comentario = signal('');
  comentando = signal(false);

  // confirmación de resolución (ticket "En revisión")
  mostrarRechazo = signal(false);
  motivoRechazo = signal('');
  resolviendo = signal(false);

  filtradas = computed(() =>
    this.incidencias().filter((i) => this.tab() === 'activo' ? i.estado !== 'Resuelto' : i.estado === 'Resuelto'));

  /** Eventos + mensajes de la incidencia, ordenados cronológicamente. */
  timeline = computed(() => {
    const d = this.detalle();
    if (!d) return [] as { tipo: 'evento' | 'mensaje'; texto: string; esAdmin: boolean; fechaUtc: string }[];
    const items = [
      ...d.eventos.map((e) => ({ tipo: 'evento' as const, texto: e.texto, esAdmin: false, fechaUtc: e.fechaUtc })),
      ...d.mensajes.map((m) => ({ tipo: 'mensaje' as const, texto: m.texto, esAdmin: m.esAdministracion, fechaUtc: m.fechaUtc })),
    ];
    return items.sort((a, b) => a.fechaUtc.localeCompare(b.fechaUtc));
  });

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.listar().subscribe({
      next: (l) => { this.incidencias.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar las incidencias.'); this.cargando.set(false); },
    });
  }

  iconoCat(c: string): string {
    return this.categorias.find((x) => x.value === c)?.icon ?? '📋';
  }
  colorCat(c: string): string {
    const map: Record<string, string> = {
      Mantenimiento: '#2563eb', Seguridad: '#dc2626', Amenidades: '#7c3aed', Mascotas: '#d97706',
      Ruido: '#0891b2', Vecinos: '#16a34a', Servicios: '#0f766e', Otro: '#64748b',
    };
    return map[c] ?? '#64748b';
  }
  colorEstado(e: string): string { return this.metaEstado[e]?.color ?? '#64748b'; }

  hace(iso: string): string {
    const s = Math.max(1, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
    if (s < 60) return 'recién';
    if (s < 3600) return `hace ${Math.floor(s / 60)} min`;
    if (s < 86400) return `hace ${Math.floor(s / 3600)} h`;
    return `hace ${Math.floor(s / 86400)} d`;
  }

  // ---- nuevo ----
  nuevo(): void {
    this.fCategoria.set('Mantenimiento');
    this.fDescripcion.set('');
    this.fArchivos().forEach((a) => URL.revokeObjectURL(a.url));
    this.fArchivos.set([]);
    this.vista.set('nuevo');
  }
  onArchivos(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (!input.files) return;
    const nuevos = Array.from(input.files).slice(0, 6 - this.fArchivos().length)
      .map((file) => ({ file, url: URL.createObjectURL(file) }));
    this.fArchivos.update((l) => [...l, ...nuevos]);
    input.value = '';
  }
  quitarArchivo(i: number): void {
    this.fArchivos.update((l) => {
      URL.revokeObjectURL(l[i].url);
      return l.filter((_, idx) => idx !== i);
    });
  }
  imagenes = computed(() => this.fArchivos().filter((a) => a.file.type.startsWith('image/')).length);
  videos = computed(() => this.fArchivos().filter((a) => a.file.type.startsWith('video/')).length);

  enviar(): void {
    if (this.fDescripcion().trim().length === 0 || this.enviando()) return;
    this.enviando.set(true);
    this.api.crear(this.fDescripcion().trim(), this.fCategoria(), this.fArchivos().map((a) => a.file)).subscribe({
      next: () => {
        this.enviando.set(false);
        this.toasts.exito('Reporte enviado a la administración');
        this.vista.set('lista');
        this.cargar();
      },
      error: (e) => { this.enviando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo enviar el reporte.'); },
    });
  }

  // ---- detalle ----
  abrir(i: Incidencia): void {
    this.detalle.set(null);
    this.imgUrls.set({});
    this.comentario.set('');
    this.mostrarRechazo.set(false);
    this.motivoRechazo.set('');
    this.vista.set('detalle');
    this.api.obtener(i.id).subscribe({
      next: (d) => {
        this.detalle.set(d);
        for (const a of d.adjuntos.filter((x) => x.esImagen)) this.cargarImagen(a.id);
      },
      error: () => { this.toasts.error('No se pudo abrir el reporte.'); this.vista.set('lista'); },
    });
  }
  private cargarImagen(id: string): void {
    this.http.get(`${environment.apiUrl}/mi-portal/incidencias/adjuntos/${id}`, { responseType: 'blob' }).subscribe({
      next: (b) => this.imgUrls.update((m) => ({ ...m, [id]: URL.createObjectURL(b) })),
      error: () => {},
    });
  }

  confirmarResuelto(): void {
    const d = this.detalle();
    if (!d || this.resolviendo()) return;
    this.resolviendo.set(true);
    this.api.confirmar(d.incidencia.id).subscribe({
      next: () => {
        this.resolviendo.set(false);
        this.toasts.exito('¡Gracias! Marcamos el reporte como resuelto.');
        this.abrir(d.incidencia);
        this.cargar();
      },
      error: (e) => { this.resolviendo.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo confirmar.'); },
    });
  }

  rechazarResuelto(): void {
    const d = this.detalle();
    if (!d || this.resolviendo()) return;
    this.resolviendo.set(true);
    this.api.rechazar(d.incidencia.id, this.motivoRechazo().trim()).subscribe({
      next: () => {
        this.resolviendo.set(false);
        this.mostrarRechazo.set(false);
        this.motivoRechazo.set('');
        this.toasts.exito('Le avisamos a la administración.');
        this.abrir(d.incidencia);
        this.cargar();
      },
      error: (e) => { this.resolviendo.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo enviar.'); },
    });
  }

  comentar(): void {
    const d = this.detalle();
    if (!d || this.comentario().trim().length === 0 || this.comentando()) return;
    this.comentando.set(true);
    const texto = this.comentario().trim();
    this.api.comentar(d.incidencia.id, texto).subscribe({
      next: () => {
        this.comentando.set(false);
        this.comentario.set('');
        this.detalle.update((cur) => cur ? {
          ...cur,
          mensajes: [...cur.mensajes, { texto, autor: 'Vos', esAdministracion: false, fechaUtc: new Date().toISOString() }],
        } : cur);
      },
      error: () => { this.comentando.set(false); this.toasts.error('No se pudo enviar el comentario.'); },
    });
  }
}
