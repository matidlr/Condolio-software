import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { forkJoin, of, switchMap } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AnuncioService } from '../../core/services/anuncio.service';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Anuncio, AnuncioDetalle, CATEGORIAS_ANUNCIO, CategoriaAnuncio, META_ANUNCIO,
} from '../../core/models/anuncio.models';
import { AuthService } from '../../core/services/auth.service';

type Orden = 'recientes' | 'antiguos';

interface ImgStage { file?: File; url: string; adjuntoId?: string; }

@Component({
  selector: 'app-anuncios',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './anuncios.component.html',
  styleUrl: './anuncios.component.scss',
  host: { '(document:click)': 'menuAbierto.set(null)' },
})
export class AnunciosComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(AnuncioService);
  private adjuntos = inject(AdjuntoService);
  private toasts = inject(ToastService);
  private sanitizer = inject(DomSanitizer);
  private auth = inject(AuthService);
  miNombre = this.auth.nombre;

  categorias = CATEGORIAS_ANUNCIO;
  meta = META_ANUNCIO;

  cargando = signal(true);
  anuncios = signal<Anuncio[]>([]);
  conteos = signal<Record<string, number>>({});
  busqueda = signal('');
  filtro = signal<CategoriaAnuncio | 'todos'>('todos');
  orden = signal<Orden>('recientes');
  portadas = signal<Record<string, string>>({});

  menuAbierto = signal<string | null>(null);

  // detalle
  detalle = signal<AnuncioDetalle | null>(null);
  detalleCargando = signal(false);
  comentarioTexto = signal('');
  enviandoComentario = signal(false);
  comEditId = signal<string | null>(null);
  comEditTexto = signal('');

  // modal
  modalAbierto = signal(false);
  editandoId = signal<string | null>(null);
  fCategoria = signal<CategoriaAnuncio>('General');
  fCuerpo = signal('');
  fFijado = signal(false);
  fEventoFecha = signal('');
  fImagenes = signal<ImgStage[]>([]);
  enviando = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  filtroCat = computed<CategoriaAnuncio | null>(() => {
    const f = this.filtro();
    return f === 'todos' ? null : f;
  });

  puedeEnviar = computed(() => this.fCuerpo().trim().length > 0 && !this.enviando());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const f = this.filtro();
    let l = this.anuncios()
      .filter((a) => f === 'todos' || a.categoria === f)
      .filter((a) => !q || (a.titulo ?? '').toLowerCase().includes(q) || a.cuerpo.toLowerCase().includes(q));
    l = [...l].sort((a, b) => {
      if (a.fijado !== b.fijado) return a.fijado ? -1 : 1;
      const cmp = a.publicadoUtc.localeCompare(b.publicadoUtc);
      return this.orden() === 'recientes' ? -cmp : cmp;
    });
    return l;
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.listar(cid).subscribe({
      next: (l) => {
        this.anuncios.set(l.anuncios);
        this.api.total.set(l.total);
        this.conteos.set({
          todos: l.total, General: l.general, Mantenimiento: l.mantenimiento,
          Urgente: l.urgente, Evento: l.evento,
        });
        this.cargando.set(false);
        for (const a of l.anuncios) this.cargarPortada(a);
      },
      error: () => { this.toasts.error('No pudimos cargar las comunicaciones.'); this.cargando.set(false); },
    });
  }

  private cargarPortada(a: Anuncio): void {
    const id = a.imagenesIds[0];
    if (!id || this.portadas()[a.id]) return;
    this.adjuntos.descargar(id).subscribe((b) =>
      this.portadas.update((p) => ({ ...p, [a.id]: URL.createObjectURL(b) })));
  }

  portada(a: Anuncio): string | null { return this.portadas()[a.id] ?? null; }

  conteo(k: string): number { return this.conteos()[k] ?? 0; }

  icono(cat: CategoriaAnuncio): SafeHtml {
    const m = META_ANUNCIO[cat];
    const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"
      stroke-linecap="round" stroke-linejoin="round">${m.icono}</svg>`;
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }

  limpiarFiltros(): void {
    this.filtro.set('todos');
    this.busqueda.set('');
  }

  // ---- modal ----
  abrirNuevo(cat?: CategoriaAnuncio): void {
    this.editandoId.set(null);
    this.fCategoria.set(cat ?? this.filtroCat() ?? 'General');
    this.fCuerpo.set('');
    this.fFijado.set(false);
    this.fEventoFecha.set('');
    this.fImagenes.set([]);
    this.modalAbierto.set(true);
  }

  editar(a: Anuncio): void {
    this.editandoId.set(a.id);
    this.fCategoria.set(a.categoria);
    this.fCuerpo.set(a.cuerpo);
    this.fFijado.set(a.fijado);
    this.fEventoFecha.set(a.eventoFechaUtc ? a.eventoFechaUtc.slice(0, 16) : '');
    this.fImagenes.set(a.imagenesIds.map((adjuntoId) => ({ adjuntoId, url: this.portadas()[a.id] ?? '' })));
    a.imagenesIds.forEach((adjId) => this.adjuntos.descargar(adjId).subscribe((b) => {
      this.fImagenes.update((l) => l.map((i) => i.adjuntoId === adjId ? { ...i, url: URL.createObjectURL(b) } : i));
    }));
    this.modalAbierto.set(true);
  }

  onArchivos(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (!input.files) return;
    const libres = 5 - this.fImagenes().length;
    for (const f of Array.from(input.files).slice(0, libres)) {
      this.fImagenes.update((l) => [...l, { file: f, url: f.type.startsWith('image/') ? URL.createObjectURL(f) : '' }]);
    }
    input.value = '';
  }
  quitarArchivo(i: number): void { this.fImagenes.update((l) => l.filter((_, idx) => idx !== i)); }
  esImg(i: ImgStage): boolean { return !!i.url; }

  aplicarFormato(tipo: 'b' | 'i' | 'li' | 'quote'): void {
    const marca = { b: '**texto**', i: '_texto_', li: '\n- ', quote: '\n> ' }[tipo];
    this.fCuerpo.update((c) => c + marca);
  }

  private cuerpoDto(imagenesIds: string[]) {
    return {
      titulo: null,
      cuerpo: this.fCuerpo().trim(),
      categoria: this.fCategoria(),
      fijado: this.fFijado(),
      publicadoUtc: null,
      eventoFechaUtc: this.fCategoria() === 'Evento' && this.fEventoFecha()
        ? new Date(this.fEventoFecha()).toISOString() : null,
      imagenesIds,
    };
  }

  enviar(): void {
    const cid = this.consorcioId();
    if (!cid || !this.puedeEnviar()) return;
    this.enviando.set(true);

    const yaSubidas = this.fImagenes().filter((i) => i.adjuntoId).map((i) => i.adjuntoId!);
    const nuevas = this.fImagenes().filter((i) => i.file);
    const editId = this.editandoId();

    const base$ = editId
      ? this.api.actualizar(cid, editId, this.cuerpoDto(yaSubidas))
      : this.api.crear(cid, this.cuerpoDto([]));

    base$.pipe(
      switchMap((anuncio) => {
        const subs = nuevas.map((i) => this.adjuntos.subir('Anuncio', anuncio.id, i.file!));
        return (subs.length ? forkJoin(subs) : of([])).pipe(
          switchMap((subidos) => {
            const idsNuevos = subidos.map((s) => s.id);
            const orden = this.fImagenes().map((i) => i.adjuntoId ?? idsNuevos.shift()).filter((x): x is string => !!x);
            return orden.length || yaSubidas.length
              ? this.api.actualizar(cid, anuncio.id, this.cuerpoDto(orden))
              : of(anuncio);
          }),
        );
      }),
    ).subscribe({
      next: () => {
        this.enviando.set(false);
        this.modalAbierto.set(false);
        this.api.refrescarTotal(cid);
        this.toasts.exito(editId ? 'Comunicación actualizada' : 'Comunicación creada');
        this.cargar(cid);
      },
      error: (e) => { this.enviando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo publicar.'); },
    });
  }

  // ---- detalle / interacciones ----
  abrirDetalle(a: Anuncio): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.detalleCargando.set(true);
    this.detalle.set({ anuncio: a, comentarios: [], likes: [] });
    this.api.obtener(cid, a.id).subscribe({
      next: (d) => { this.detalle.set(d); this.detalleCargando.set(false); },
      error: () => { this.detalleCargando.set(false); this.toasts.error('No se pudo abrir la comunicación.'); },
    });
  }

  cerrarDetalle(): void {
    this.detalle.set(null);
    this.comentarioTexto.set('');
    this.comEditId.set(null);
    const cid = this.consorcioId();
    if (cid) this.cargar(cid);
  }

  private recargarDetalle(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    if (cid && d) this.api.obtener(cid, d.anuncio.id).subscribe((x) => this.detalle.set(x));
  }

  like(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    if (!cid || !d) return;
    this.api.toggleLike(cid, d.anuncio.id).subscribe((r) => {
      this.detalle.set({
        ...d,
        anuncio: { ...d.anuncio, totalLikes: r.total, yoDiLike: r.yoDiLike },
        likes: r.likes,
      });
    });
  }

  enviarComentario(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    const txt = this.comentarioTexto().trim();
    if (!cid || !d || !txt || this.enviandoComentario()) return;
    this.enviandoComentario.set(true);
    this.api.comentar(cid, d.anuncio.id, txt).subscribe({
      next: () => { this.enviandoComentario.set(false); this.comentarioTexto.set(''); this.recargarDetalle(); },
      error: (e) => { this.enviandoComentario.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo comentar.'); },
    });
  }

  empezarEdicionComentario(c: { id: string; texto: string }): void {
    this.comEditId.set(c.id);
    this.comEditTexto.set(c.texto);
  }
  guardarEdicionComentario(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    const id = this.comEditId();
    const txt = this.comEditTexto().trim();
    if (!cid || !d || !id || !txt) return;
    this.api.editarComentario(cid, d.anuncio.id, id, txt).subscribe({
      next: () => { this.comEditId.set(null); this.recargarDetalle(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo editar.'),
    });
  }
  borrarComentario(c: { id: string }): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    if (!cid || !d || !confirm('¿Eliminar este comentario?')) return;
    this.api.eliminarComentario(cid, d.anuncio.id, c.id).subscribe({
      next: () => this.recargarDetalle(),
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }

  editarDesdeDetalle(): void {
    const d = this.detalle();
    if (!d) return;
    this.detalle.set(null);
    this.editar(d.anuncio);
  }

  toggleFijar(a: Anuncio): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.fijar(cid, a.id, !a.fijado).subscribe({
      next: () => { this.toasts.exito(a.fijado ? 'Comunicación desfijada' : 'Comunicación fijada'); this.cargar(cid); },
      error: () => this.toasts.error('No se pudo fijar.'),
    });
  }

  eliminar(a: Anuncio): void {
    const cid = this.consorcioId();
    if (!cid || !confirm('¿Eliminar esta comunicación?')) return;
    this.api.eliminar(cid, a.id).subscribe({
      next: () => { this.api.refrescarTotal(cid); this.toasts.exito('Comunicación eliminada'); this.cargar(cid); },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }
}
