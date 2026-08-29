import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Observable } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { DocumentoService } from '../../core/services/documento.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Analiticas, Carpeta, CATEGORIAS_DOCUMENTO, CategoriaDocumento, Contenido, Documento,
  LABEL_CATEGORIA_DOC, META_NIVEL, NIVELES_ACCESO, NIVELES_COMPARTIR, NivelAcceso,
} from '../../core/models/documento.models';

type Seccion = 'inicio' | 'recientes' | 'destacados' | 'analiticas'
  | 'nivel-Admin' | 'nivel-Propietarios' | 'nivel-Todos';

const COLORES_CAT = ['#e8613c', '#1f9d8e', '#1f3a4d', '#e0a83d', '#7c3aed', '#2563eb', '#94a3b8'];

@Component({
  selector: 'app-documentos',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './documentos.component.html',
  styleUrl: './documentos.component.scss',
  host: { '(document:click)': 'menu.set(null); nuevoMenu.set(false)' },
})
export class DocumentosComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(DocumentoService);
  private toasts = inject(ToastService);

  niveles = NIVELES_ACCESO;
  nivelesCompartir = NIVELES_COMPARTIR;
  categoriasDoc = CATEGORIAS_DOCUMENTO;
  labelCat = LABEL_CATEGORIA_DOC;
  meta = META_NIVEL;

  cargando = signal(true);
  seccion = signal<Seccion>('inicio');
  carpetaId = signal<string | null>(null);
  ruta = signal<{ id: string | null; nombre: string }[]>([{ id: null, nombre: 'Documentos' }]);
  contenido = signal<Contenido | null>(null);
  listaPlana = signal<Documento[]>([]);
  busqueda = signal('');
  vista = signal<'grid' | 'lista'>('grid');
  menu = signal<string | null>(null);
  nuevoMenu = signal(false);
  subiendo = signal(false);

  // modales
  carpetaModal = signal(false);
  nuevaCarpetaNombre = signal('');
  subirModal = signal(false);
  subirArchivos = signal<{ file: File; nombre: string; ext: string; categoria: CategoriaDocumento; nivel: NivelAcceso; abierto: boolean }[]>([]);
  bulkCategoria = signal<CategoriaDocumento | ''>('');
  bulkNivel = signal<NivelAcceso | ''>('');
  subirProgreso = signal<{ nombre: string; tipo: string; hecho: boolean; error: boolean }[] | null>(null);
  subirProgresoHechos = computed(() => this.subirProgreso()?.filter((p) => p.hecho).length ?? 0);
  subirTerminado = computed(() => {
    const p = this.subirProgreso();
    return !!p && p.every((x) => x.hecho || x.error);
  });
  renombrar = signal<{ tipo: 'carpeta' | 'documento'; item: Carpeta | Documento } | null>(null);
  renombreTxt = signal('');
  eliminarConfirm = signal<{ tipo: 'carpeta' | 'documento'; item: Carpeta | Documento } | null>(null);
  mover = signal<{ tipo: 'carpeta' | 'documento'; item: Carpeta | Documento } | null>(null);
  moverDestino = signal<string | null>(null);
  todasCarpetas = signal<Carpeta[]>([]);
  editDoc = signal<Documento | null>(null);
  editNombre = signal('');
  editNivel = signal<NivelAcceso>('Todos');
  editCategoria = signal<CategoriaDocumento>('General');

  compartirDoc = signal<Documento | null>(null);
  compartirNivel = signal<NivelAcceso>('Admin');

  // analíticas
  analiticas = signal<Analiticas | null>(null);
  analiticasTab = signal<'resumen' | 'popular' | 'actividad' | 'visores'>('resumen');
  hoverCat = signal<number | null>(null);
  hoverBar = signal<number | null>(null);
  hoverPunto = signal<number | null>(null);

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
    if (!c) return { pct: 0, usado: '0 B', total: '10 GB' };
    return {
      pct: Math.min(100, Math.round((c.almacenamientoUsado / c.almacenamientoTotal) * 100)),
      usado: this.tamano(c.almacenamientoUsado),
      total: this.tamano(c.almacenamientoTotal),
    };
  });

  // ---- geometría de gráficos de analíticas ----
  private readonly donutC = 2 * Math.PI * 54;

  catDonut = computed(() => {
    const cats = this.analiticas()?.porCategoria ?? [];
    const total = cats.reduce((s, c) => s + c.tamano, 0) || 1;
    let acc = 0;
    return cats.map((c, i) => {
      const pct = c.tamano / total;
      const seg = {
        label: this.labelCat[c.categoria], color: COLORES_CAT[i % COLORES_CAT.length],
        cantidad: c.cantidad, tamano: c.tamano, pct: Math.round(pct * 100),
        dash: `${pct * this.donutC} ${this.donutC}`, offset: -acc * this.donutC,
      };
      acc += pct;
      return seg;
    });
  });

  catBarras = computed(() => {
    const cats = this.analiticas()?.porCategoria ?? [];
    const max = Math.max(1, ...cats.map((c) => c.tamano));
    return cats.map((c, i) => ({
      label: this.labelCat[c.categoria], color: COLORES_CAT[i % COLORES_CAT.length],
      tamano: c.tamano, hPct: Math.round((c.tamano / max) * 100),
    }));
  });

  catBarrasMax = computed(() => Math.max(1, ...(this.analiticas()?.porCategoria ?? []).map((c) => c.tamano)));

  timeline = computed(() => {
    const pts = this.analiticas()?.timeline ?? [];
    const w = 620, h = 200, padX = 42, padY = 16;
    const max = Math.max(1, ...pts.map((p) => Math.max(p.vistas, p.descargas)));
    const stepX = pts.length > 1 ? (w - padX * 2) / (pts.length - 1) : 0;
    const y = (v: number) => h - padY - (v / max) * (h - padY * 2);
    const x = (i: number) => padX + i * stepX;
    const puntos = pts.map((p, i) => ({ ...p, cx: x(i), cyV: y(p.vistas), cyD: y(p.descargas) }));
    return {
      w, h, max,
      lineaV: puntos.map((p) => `${p.cx},${p.cyV}`).join(' '),
      lineaD: puntos.map((p) => `${p.cx},${p.cyD}`).join(' '),
      puntos,
      ejeY: [0, 0.25, 0.5, 0.75, 1].map((f) => ({ v: Math.round(max * f), y: y(max * f) })),
    };
  });

  catHover = computed(() => {
    const i = this.hoverCat();
    return i === null ? null : this.catDonut()[i] ?? null;
  });
  puntoHover = computed(() => {
    const i = this.hoverPunto();
    return i === null ? null : this.timeline().puntos[i] ?? null;
  });

  moverDisponibles = computed(() => {
    const m = this.mover();
    if (!m || m.tipo === 'documento') return this.todasCarpetas();
    return this.todasCarpetas().filter((c) => c.id !== m.item.id);
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.cargando.set(true);
    const s = this.seccion();
    if (s === 'analiticas') {
      this.api.analiticas(cid).subscribe({
        next: (a) => { this.analiticas.set(a); this.cargando.set(false); },
        error: () => { this.toasts.error('No pudimos cargar las analíticas.'); this.cargando.set(false); },
      });
      this.api.listar(cid, null).subscribe((c) => this.contenido.set({ ...c, carpetas: [], documentos: [] }));
      return;
    }
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
      this.api.listar(cid, null).subscribe((c) => this.contenido.set({ ...c, carpetas: [], documentos: [] }));
    }
  }

  irSeccion(s: Seccion): void {
    this.seccion.set(s);
    this.carpetaId.set(null);
    this.ruta.set([{ id: null, nombre: 'Documentos' }]);
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
    if (s === 'analiticas') return 'Analíticas';
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

  // ---- crear carpeta ----
  abrirCarpetaModal(): void {
    this.nuevoMenu.set(false);
    this.nuevaCarpetaNombre.set('');
    this.carpetaModal.set(true);
  }
  crearCarpeta(): void {
    const cid = this.consorcioId();
    const n = this.nuevaCarpetaNombre().trim();
    if (!cid || !n) return;
    this.api.crearCarpeta(cid, { nombre: n, carpetaPadreId: this.carpetaId(), nivel: 'Todos' }).subscribe({
      next: () => { this.carpetaModal.set(false); this.toasts.exito('Carpeta creada'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo crear.'),
    });
  }

  // ---- renombrar (carpeta o documento) ----
  abrirRenombrar(tipo: 'carpeta' | 'documento', item: Carpeta | Documento): void {
    this.renombrar.set({ tipo, item });
    this.renombreTxt.set(item.nombre);
  }
  confirmarRenombre(): void {
    const cid = this.consorcioId();
    const r = this.renombrar();
    const n = this.renombreTxt().trim();
    if (!cid || !r || !n) return;
    const obs: Observable<unknown> = r.tipo === 'carpeta'
      ? this.api.renombrarCarpeta(cid, r.item.id, n)
      : this.api.actualizar(cid, r.item.id, {
          nombre: n, nivel: (r.item as Documento).nivel, categoria: (r.item as Documento).categoria,
          carpetaId: (r.item as Documento).carpetaId,
        });
    obs.subscribe({
      next: () => { this.renombrar.set(null); this.toasts.exito('Renombrado'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo renombrar.'),
    });
  }

  // ---- mover (carpeta o documento) ----
  abrirMover(tipo: 'carpeta' | 'documento', item: Carpeta | Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.mover.set({ tipo, item });
    this.moverDestino.set(null);
    this.api.todasLasCarpetas(cid).subscribe((l) => this.todasCarpetas.set(l));
  }
  confirmarMover(): void {
    const cid = this.consorcioId();
    const m = this.mover();
    if (!cid || !m) return;
    const obs: Observable<unknown> = m.tipo === 'carpeta'
      ? this.api.moverCarpeta(cid, m.item.id, this.moverDestino())
      : this.api.actualizar(cid, m.item.id, {
          nombre: (m.item as Documento).nombre, nivel: (m.item as Documento).nivel,
          categoria: (m.item as Documento).categoria, carpetaId: this.moverDestino(),
        });
    obs.subscribe({
      next: () => { this.mover.set(null); this.toasts.exito('Movido'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo mover.'),
    });
  }

  preview = signal<{ doc: Documento; url: string } | null>(null);

  vistaPrevia(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.descargar(cid, d.id).subscribe((blob) => {
      const prev = this.preview();
      if (prev) URL.revokeObjectURL(prev.url);
      this.preview.set({ doc: d, url: URL.createObjectURL(blob) });
    });
  }
  cerrarPreview(): void {
    const prev = this.preview();
    if (prev) URL.revokeObjectURL(prev.url);
    this.preview.set(null);
  }
  abrirEnPestana(): void {
    const prev = this.preview();
    if (prev) window.open(prev.url, '_blank');
  }
  esImagen(ct: string): boolean { return ct.startsWith('image/'); }
  esPdf(ct: string): boolean { return ct === 'application/pdf'; }
  compartir(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    const url = `${location.origin}/panel/documentos?doc=${d.id}`;
    navigator.clipboard?.writeText(url).then(
      () => this.toasts.exito('Enlace copiado'),
      () => this.toasts.info(url),
    );
  }

  // ---- eliminar ----
  pedirEliminar(tipo: 'carpeta' | 'documento', item: Carpeta | Documento): void {
    this.eliminarConfirm.set({ tipo, item });
  }
  confirmarEliminar(): void {
    const cid = this.consorcioId();
    const e = this.eliminarConfirm();
    if (!cid || !e) return;
    const obs = e.tipo === 'carpeta'
      ? this.api.eliminarCarpeta(cid, e.item.id)
      : this.api.eliminar(cid, e.item.id);
    obs.subscribe({
      next: () => { this.eliminarConfirm.set(null); this.toasts.exito('Eliminado'); this.cargar(); },
      error: (err) => this.toasts.error(err?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  // ---- subir ----
  abrirSubirModal(): void {
    this.nuevoMenu.set(false);
    this.subirArchivos.set([]);
    this.bulkCategoria.set('');
    this.bulkNivel.set('');
    this.subirModal.set(true);
  }
  onArchivosElegidos(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (input.files) {
      const nuevos = Array.from(input.files).slice(0, 10 - this.subirArchivos().length).map((file) => {
        const punto = file.name.lastIndexOf('.');
        return {
          file,
          nombre: punto > 0 ? file.name.slice(0, punto) : file.name,
          ext: punto > 0 ? file.name.slice(punto) : '',
          categoria: 'General' as CategoriaDocumento,
          nivel: 'Todos' as NivelAcceso,
          abierto: this.subirArchivos().length === 0,
        };
      });
      this.subirArchivos.update((l) => [...l, ...nuevos]);
    }
    input.value = '';
  }
  quitarArchivo(i: number): void { this.subirArchivos.update((l) => l.filter((_, idx) => idx !== i)); }
  toggleArchivo(i: number): void {
    this.subirArchivos.update((l) => l.map((a, idx) => idx === i ? { ...a, abierto: !a.abierto } : a));
  }
  setArchivo(i: number, campo: 'nombre' | 'categoria' | 'nivel', valor: string): void {
    this.subirArchivos.update((l) => l.map((a, idx) => idx === i ? { ...a, [campo]: valor } : a));
  }
  aplicarBulk(): void {
    const cat = this.bulkCategoria(); const niv = this.bulkNivel();
    this.subirArchivos.update((l) => l.map((a) => ({
      ...a,
      categoria: cat ? cat : a.categoria,
      nivel: niv ? niv : a.nivel,
    })));
  }
  volverSubir(): void { this.subirArchivos.set([]); }

  confirmarSubir(): void {
    const cid = this.consorcioId();
    const archivos = this.subirArchivos();
    if (!cid || !archivos.length) return;
    this.subiendo.set(true);
    this.subirProgreso.set(archivos.map((a) => ({ nombre: a.nombre + a.ext, tipo: a.file.type, hecho: false, error: false })));
    archivos.forEach((a, i) => {
      const file = a.nombre + a.ext !== a.file.name
        ? new File([a.file], a.nombre + a.ext, { type: a.file.type })
        : a.file;
      this.api.subir(cid, file, this.carpetaId(), a.nivel, a.categoria).subscribe({
        next: () => this.marcarProgreso(i, false),
        error: (e) => { this.marcarProgreso(i, true); this.toasts.error(e?.error?.message ?? `No se pudo subir ${a.nombre}.`); },
      });
    });
  }

  private marcarProgreso(i: number, error: boolean): void {
    this.subirProgreso.update((p) => {
      if (!p) return p;
      const c = [...p]; c[i] = { ...c[i], hecho: !error, error }; return c;
    });
    if (this.subirTerminado()) { this.subiendo.set(false); this.cargar(); }
  }

  cerrarSubir(): void {
    this.subirModal.set(false);
    this.subirProgreso.set(null);
    this.subirArchivos.set([]);
  }

  // ---- documento ----
  descargar(d: Documento): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.descargar(cid, d.id, true).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
      if (this.seccion() === 'analiticas') this.cargar();
    });
  }

  // ---- compartir (nivel de acceso) ----
  abrirCompartir(d: Documento): void {
    this.compartirDoc.set(d);
    this.compartirNivel.set(d.nivel);
  }
  guardarCompartir(): void {
    const cid = this.consorcioId();
    const d = this.compartirDoc();
    if (!cid || !d) return;
    this.api.actualizar(cid, d.id, {
      nombre: d.nombre, nivel: this.compartirNivel(), categoria: d.categoria, carpetaId: d.carpetaId,
    }).subscribe({
      next: () => { this.compartirDoc.set(null); this.toasts.exito('Acceso actualizado'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'),
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
    this.editCategoria.set(d.categoria);
  }
  guardarEdit(): void {
    const cid = this.consorcioId();
    const d = this.editDoc();
    if (!cid || !d || !this.editNombre().trim()) return;
    this.api.actualizar(cid, d.id, {
      nombre: this.editNombre().trim(), nivel: this.editNivel(), categoria: this.editCategoria(), carpetaId: d.carpetaId,
    }).subscribe({
      next: () => { this.editDoc.set(null); this.toasts.exito('Documento actualizado'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'),
    });
  }
}
