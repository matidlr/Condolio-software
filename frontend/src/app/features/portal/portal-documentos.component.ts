import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MiDocumentoService } from '../../core/services/mi-documento.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Carpeta, CATEGORIAS_DOCUMENTO, CategoriaDocumento, Contenido, Documento, LABEL_CATEGORIA_DOC,
} from '../../core/models/documento.models';

const CHIPS: { value: CategoriaDocumento | 'todos'; label: string }[] = [
  { value: 'todos', label: 'Todos' },
  { value: 'Financiero', label: 'Financiero' },
  { value: 'LegalYContratos', label: 'Legal' },
  { value: 'ReglasYRegulaciones', label: 'Reglamento' },
  { value: 'Recibos', label: 'Recibos' },
  { value: 'Mantenimiento', label: 'Mantenimiento' },
  { value: 'ActasReuniones', label: 'Actas' },
  { value: 'General', label: 'General' },
];

type Orden = 'recientes' | 'antiguos' | 'az' | 'za' | 'grandes' | 'chicos';
const ORDENES: { value: Orden; label: string }[] = [
  { value: 'recientes', label: 'Más recientes' },
  { value: 'antiguos', label: 'Más antiguos' },
  { value: 'az', label: 'Nombre (A-Z)' },
  { value: 'za', label: 'Nombre (Z-A)' },
  { value: 'grandes', label: 'Más grandes' },
  { value: 'chicos', label: 'Más pequeños' },
];

@Component({
  selector: 'app-portal-documentos',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-documentos.component.html',
  styleUrl: './portal-documentos.component.scss',
  host: { '(document:click)': 'ordenMenu.set(false)' },
})
export class PortalDocumentosComponent {
  private api = inject(MiDocumentoService);
  private toasts = inject(ToastService);

  chips = CHIPS;
  labelCat = LABEL_CATEGORIA_DOC;

  cargando = signal(true);
  contenido = signal<Contenido | null>(null);
  sel = signal<Documento | null>(null);
  ruta = signal<{ id: string | null; nombre: string }[]>([{ id: null, nombre: 'Documentos' }]);
  busqueda = signal('');
  chip = signal<CategoriaDocumento | 'todos'>('todos');
  ordenes = ORDENES;
  orden = signal<Orden>('recientes');
  ordenMenu = signal(false);
  vista = signal<'lista' | 'grid'>('lista');

  ordenLabel = computed(() => ORDENES.find((o) => o.value === this.orden())?.label ?? '');

  carpetaActual = computed(() => this.ruta()[this.ruta().length - 1].id);

  carpetasVis = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return (this.contenido()?.carpetas ?? []).filter((c) => !q || c.nombre.toLowerCase().includes(q));
  });
  docsVis = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const cat = this.chip();
    const l = (this.contenido()?.documentos ?? [])
      .filter((d) => (cat === 'todos' || d.categoria === cat) && (!q || d.nombre.toLowerCase().includes(q)));
    const o = this.orden();
    return [...l].sort((a, b) => {
      switch (o) {
        case 'recientes': return b.creadoUtc.localeCompare(a.creadoUtc);
        case 'antiguos': return a.creadoUtc.localeCompare(b.creadoUtc);
        case 'az': return a.nombre.localeCompare(b.nombre);
        case 'za': return b.nombre.localeCompare(a.nombre);
        case 'grandes': return b.tamano - a.tamano;
        case 'chicos': return a.tamano - b.tamano;
      }
    });
  });

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.listar(this.carpetaActual()).subscribe({
      next: (c) => { this.contenido.set(c); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar los documentos.'); this.cargando.set(false); },
    });
  }

  abrirCarpeta(c: Carpeta): void {
    this.ruta.update((r) => [...r, { id: c.id, nombre: c.nombre }]);
    this.cargar();
  }
  irRuta(i: number): void {
    this.ruta.update((r) => r.slice(0, i + 1));
    this.cargar();
  }

  icono(ct: string): string {
    if (ct.startsWith('image/')) return '🖼';
    if (ct === 'application/pdf') return '📕';
    if (ct.includes('word') || ct.includes('document')) return '📘';
    if (ct.includes('sheet') || ct.includes('excel')) return '📗';
    return '📄';
  }
  tamano(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  abrir(d: Documento): void { this.sel.set(d); }
  cerrarDetalle(): void { this.sel.set(null); }

  tipoArchivo(ct: string): string {
    if (ct === 'application/pdf') return 'PDF';
    if (ct.startsWith('image/')) return 'Imagen';
    if (ct.includes('word') || ct.includes('document')) return 'Word';
    if (ct.includes('sheet') || ct.includes('excel')) return 'Excel';
    if (ct.startsWith('text/')) return 'Texto';
    return ct || 'Archivo';
  }
  esPdf(ct: string): boolean { return ct === 'application/pdf'; }

  ver(d: Documento): void {
    this.api.descargar(d.id, false).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => this.toasts.error('No se pudo abrir el documento.'),
    });
  }
  descargar(d: Documento, ev?: Event): void {
    ev?.stopPropagation();
    this.api.descargar(d.id, true).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url; a.download = d.nombre; a.click();
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => this.toasts.error('No se pudo descargar.'),
    });
  }
  async compartir(d: Documento): Promise<void> {
    const nav = navigator as Navigator & { canShare?: (x: ShareData) => boolean };
    this.api.descargar(d.id, false).subscribe({
      next: async (blob) => {
        const file = new File([blob], d.nombre, { type: d.contentType || 'application/octet-stream' });
        try {
          if (nav.canShare?.({ files: [file] })) {
            await nav.share({ files: [file], title: d.nombre });
            return;
          }
          if (navigator.share) { await navigator.share({ title: d.nombre, text: d.nombre }); return; }
        } catch { return; }
        this.descargar(d);
      },
      error: () => this.toasts.error('No se pudo compartir.'),
    });
  }
  copiarEnlace(d: Documento): void {
    const url = `${location.origin}/portal/documentos?doc=${d.id}`;
    navigator.clipboard?.writeText(url).then(
      () => this.toasts.exito('Enlace copiado'),
      () => this.toasts.info(url),
    );
  }
}
