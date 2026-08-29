import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { DocumentoService } from '../../core/services/documento.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Carpeta, Contenido, Documento, META_NIVEL, NIVELES_ACCESO, NivelAcceso,
} from '../../core/models/documento.models';

type Seccion = 'inicio' | 'recientes' | 'destacados' | 'nivel-Admin' | 'nivel-Propietarios' | 'nivel-Todos';

@Component({
  selector: 'app-documentos',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './documentos.component.html',
  styleUrl: './documentos.component.scss',
  host: { '(document:click)': 'menu.set(null)' },
})
export class DocumentosComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(DocumentoService);
  private toasts = inject(ToastService);

  niveles = NIVELES_ACCESO;
  meta = META_NIVEL;

  cargando = signal(true);
  seccion = signal<Seccion>('inicio');
  carpetaId = signal<string | null>(null);
  ruta = signal<{ id: string | null; nombre: string }[]>([{ id: null, nombre: 'Inicio' }]);
  contenido = signal<Contenido | null>(null);
  listaPlana = signal<Documento[]>([]);
  busqueda = signal('');
  vista = signal<'grid' | 'lista'>('grid');
  menu = signal<string | null>(null);

  // modales
  carpetaModal = signal(false);
  nuevaCarpetaNombre = signal('');
  nuevaCarpetaNivel = signal<NivelAcceso>('Todos');
  subirNivel = signal<NivelAcceso>('Todos');
  nuevoMenu = signal(false);
  subiendo = signal(false);
  editDoc = signal<Documento | null>(null);
  editNombre = signal('');
  editNivel = signal<NivelAcceso>('Todos');

  private consorcioId = computed(() => this.consorcios.activoId());

  esInicio = computed(() => this.seccion() === 'inicio');

  carpetasVis = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return (this.contenido()?.carpetas ?? []).filter((c) => !q || c.nombre.toLowerCase().includes(q));
  });
  docsVis = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const base = this.esInicio() ? (this.contenido()?.documentos ?? []) : this.listaPlana();
    return base.filter((d) => !q || d.nombre.toLowerCase().includes(q));
  });

  almacenamiento = computed(() => {
    const c = this.contenido();
    if (!c) return { pct: 0, usado: '0', total: '10 GB' };
    return {
      pct: Math.min(100, Math.round((c.almacenamientoUsado / c.almacenamientoTotal) * 100)),
      usado: this.tamano(c.almacenamientoUsado),
      total: this.tamano(c.almacenamientoTotal),
    };
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.cargando.set(true);
    const s = this.seccion();
    if (s === 'inicio') {
      this.api.listar(cid, this.carpetaId()).subscribe({
        next: (c) => { this.contenido.set(c); this.cargando.set(false); },
        error: () => { this.toasts.error('No pudimos cargar los documentos.'); this.cargando.set(false); },
      });
    } else {
      const obs = s === 'recientes' ? this.api.recientes(cid)
        : s === 'destacados' ? this.api.destacados(cid)
        : this.api.porNivel(cid, s.replace('nivel-', '') as NivelAcceso);
      obs.subscribe({
        next: (l) => { this.listaPlana.set(l); this.cargando.set(false); },
        error: () => { this.toasts.error('No pudimos cargar.'); this.cargando.set(false); },
      });
      // seguimos mostrando el almacenamiento
      this.api.listar(cid, null).subscribe((c) => this.contenido.set({ ...c, carpetas: [], documentos: [] }));
    }
  }

  irSeccion(s: Seccion): void {
    this.seccion.set(s);
    this.carpetaId.set(null);
    this.ruta.set([{ id: null, nombre: 'Inicio' }]);
    this.cargar();
  }

  abrirCarpeta(c: Carpeta): void {
    this.seccion.set('inicio');
    this.carpetaId.set(c.id);
    this.ruta.update((r) => [...r, { id: c.id, nombre: c.nombre }]);
    this.cargar();
  }
  irRuta(i: number): void {
    const r = this.ruta().slice(0, i + 1);
    this.ruta.set(r);
    this.carpetaId.set(r[r.length - 1].id);
    this.seccion.set('inicio');
    this.cargar();
  }

  seccionTitulo(): string {
    const s = this.seccion();
    if (s === 'inicio') return this.ruta()[this.ruta().length - 1].nombre;
    if (s === 'recientes') return 'Recientes';
    if (s === 'destacados') return 'Destacados';
    return this.meta[s.replace('nivel-', '') as NivelAcceso].label;
  }

  // ---- helpers ----
  tamano(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
    return `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`;
  }
  icono(ct: string): string {
    if (ct.startsWith('image/')) return '🖼';
    if (ct === 'application/pdf') return '📕';
    if (ct.includes('word') || ct.includes('document')) return '📘';
    if (ct.includes('sheet') || ct.includes('excel')) return '📗';
    if (ct.includes('zip') || ct.includes('compressed')) return '🗜';
    return '📄';
  }

  // ---- carpeta ----
  abrirCarpetaModal(): void {
    this.nuevoMenu.set(false);
    this.nuevaCarpetaNombre.set('');
    this.nuevaCarpetaNivel.set('Todos');
    this.carpetaModal.set(true);
  }
  crearCarpeta(): void {
    const cid = this.consorcioId();
    const n = this.nuevaCarpetaNombre().trim();
    if (!cid || !n) return;
    this.api.crearCarpeta(cid, { nombre: n, carpetaPadreId: this.carpetaId(), nivel: this.nuevaCarpetaNivel() }).subscribe({
      next: () => { this.carpetaModal.set(false); this.toasts.exito('Carpeta creada'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo crear.'),
    });
  }
  renombrarCarpeta(c: Carpeta): void {
    const cid = this.consorcioId();
    const nuevo = prompt('Nuevo nombre', c.nombre);
    if (!cid || !nuevo?.trim()) return;
    this.api.renombrarCarpeta(cid, c.id, nuevo.trim()).subscribe({
      next: () => { this.toasts.exito('Carpeta renombrada'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo renombrar.'),
    });
  }
  eliminarCarpeta(c: Carpeta): void {
    const cid = this.consorcioId();
    if (!cid || !confirm(`¿Eliminar la carpeta "${c.nombre}"?`)) return;
    this.api.eliminarCarpeta(cid, c.id).subscribe({
      next: () => { this.toasts.exito('Carpeta eliminada'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  // ---- documento ----
  onSubir(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const cid = this.consorcioId();
    if (!cid || !input.files?.length) return;
    this.nuevoMenu.set(false);
    this.subiendo.set(true);
    const archivos = Array.from(input.files);
    let pendientes = archivos.length;
    for (const f of archivos) {
      this.api.subir(cid, f, this.carpetaId(), this.subirNivel()).subscribe({
        next: () => { if (--pendientes === 0) { this.subiendo.set(false); this.toasts.exito('Archivo(s) subido(s)'); this.cargar(); } },
        error: (e) => { this.subiendo.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo subir.'); },
      });
    }
    input.value = '';
  }

  descargar(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.descargar(cid, d.id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = d.nombre; a.click();
      URL.revokeObjectURL(url);
    });
  }
  toggleDestacar(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.destacar(cid, d.id, !d.destacado).subscribe({
      next: () => { this.toasts.exito(d.destacado ? 'Quitado de destacados' : 'Marcado como destacado'); this.cargar(); },
      error: () => this.toasts.error('No se pudo actualizar.'),
    });
  }
  abrirEdit(d: Documento): void {
    this.editDoc.set(d);
    this.editNombre.set(d.nombre);
    this.editNivel.set(d.nivel);
  }
  guardarEdit(): void {
    const cid = this.consorcioId();
    const d = this.editDoc();
    if (!cid || !d || !this.editNombre().trim()) return;
    this.api.actualizar(cid, d.id, { nombre: this.editNombre().trim(), nivel: this.editNivel(), carpetaId: d.carpetaId }).subscribe({
      next: () => { this.editDoc.set(null); this.toasts.exito('Documento actualizado'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'),
    });
  }
  eliminar(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid || !confirm(`¿Eliminar "${d.nombre}"?`)) return;
    this.api.eliminar(cid, d.id).subscribe({
      next: () => { this.toasts.exito('Documento eliminado'); this.cargar(); },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }
}
