import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { MiMuroService, MuroDetalle } from '../../core/services/mi-muro.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Anuncio } from '../../core/models/anuncio.models';

@Component({
  selector: 'app-portal-muro',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-muro.component.html',
  styleUrl: './portal-muro.component.scss',
  host: { '(document:click)': 'menuComentario.set(null)' },
})
export class PortalMuroComponent {
  private api = inject(MiMuroService);
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private toasts = inject(ToastService);

  miNombre = this.auth.nombre;

  vista = signal<'feed' | 'detalle' | 'crear'>('feed');
  cargando = signal(true);
  posts = signal<Anuncio[]>([]);
  imgUrls = signal<Record<string, string>>({});

  // crear
  nuevoCuerpo = signal('');
  nuevasImgs = signal<{ file: File; url: string }[]>([]);
  publicando = signal(false);

  // detalle
  detalle = signal<MuroDetalle | null>(null);
  comentario = signal('');
  comentando = signal(false);
  menuComentario = signal<string | null>(null);
  editandoComentario = signal<string | null>(null);
  editTexto = signal('');
  confirmarEliminar = signal<string | null>(null);

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.feed().subscribe({
      next: (l) => {
        this.posts.set(l);
        this.cargando.set(false);
        for (const p of l) for (const id of p.imagenesIds) this.cargarImg(id);
      },
      error: () => { this.toasts.error('No pudimos cargar el muro.'); this.cargando.set(false); },
    });
  }
  private cargarImg(id: string): void {
    if (this.imgUrls()[id]) return;
    this.http.get(`${environment.apiUrl}/mi-portal/muro/adjuntos/${id}`, { responseType: 'blob' }).subscribe({
      next: (b) => this.imgUrls.update((m) => ({ ...m, [id]: URL.createObjectURL(b) })),
      error: () => {},
    });
  }
  img(id: string): string | null { return this.imgUrls()[id] ?? null; }

  inicial(n: string): string { return (n || '?').trim().split(' ').map((x) => x[0] ?? '').slice(0, 2).join('').toUpperCase(); }
  hace(iso: string): string {
    const h = Math.floor((Date.now() - new Date(iso).getTime()) / 3_600_000);
    if (h < 1) return 'hace instantes';
    if (h < 24) return `hace ~${h}h`;
    const d = Math.floor(h / 24);
    return d < 30 ? `hace ${d}d` : new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
  }

  // ---- feed acciones ----
  toggleLike(p: Anuncio, ev?: Event): void {
    ev?.stopPropagation();
    this.api.toggleLike(p.id).subscribe({
      next: (r) => {
        this.posts.update((l) => l.map((x) => x.id === p.id ? { ...x, totalLikes: r.total, yoDiLike: r.yoDiLike } : x));
        this.detalle.update((d) => d && d.anuncio.id === p.id
          ? { ...d, anuncio: { ...d.anuncio, totalLikes: r.total, yoDiLike: r.yoDiLike }, likes: r.likes }
          : d);
      },
      error: () => {},
    });
  }
  compartir(p: Anuncio, ev?: Event): void {
    ev?.stopPropagation();
    const txt = `${p.autor}: ${p.cuerpo}`;
    if (navigator.share) navigator.share({ text: txt }).catch(() => {});
    else navigator.clipboard?.writeText(txt).then(() => this.toasts.exito('Copiado'));
  }

  // ---- componer ----
  abrirComponer(): void {
    this.nuevoCuerpo.set('');
    this.nuevasImgs().forEach((i) => URL.revokeObjectURL(i.url));
    this.nuevasImgs.set([]);
    this.vista.set('crear');
  }
  onImgs(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (!input.files) return;
    const nuevas = Array.from(input.files).slice(0, 4 - this.nuevasImgs().length)
      .map((file) => ({ file, url: URL.createObjectURL(file) }));
    this.nuevasImgs.update((l) => [...l, ...nuevas]);
    input.value = '';
  }
  quitarImg(i: number): void {
    this.nuevasImgs.update((l) => { URL.revokeObjectURL(l[i].url); return l.filter((_, idx) => idx !== i); });
  }
  publicar(): void {
    if ((this.nuevoCuerpo().trim().length === 0 && this.nuevasImgs().length === 0) || this.publicando()) return;
    this.publicando.set(true);
    this.api.publicar(this.nuevoCuerpo().trim(), this.nuevasImgs().map((i) => i.file)).subscribe({
      next: () => {
        this.publicando.set(false);
        this.vista.set('feed');
        this.toasts.exito('Publicado');
        this.cargar();
      },
      error: (e) => { this.publicando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo publicar.'); },
    });
  }

  // ---- detalle ----
  abrir(p: Anuncio): void {
    this.detalle.set(null);
    this.comentario.set('');
    this.vista.set('detalle');
    this.api.publicacion(p.id).subscribe({
      next: (d) => {
        this.detalle.set(d);
        for (const id of d.anuncio.imagenesIds) this.cargarImg(id);
      },
      error: () => { this.toasts.error('No se pudo abrir la publicación.'); this.vista.set('feed'); },
    });
  }

  comentar(): void {
    const d = this.detalle();
    if (!d || this.comentario().trim().length === 0 || this.comentando()) return;
    this.comentando.set(true);
    this.api.comentar(d.anuncio.id, this.comentario().trim()).subscribe({
      next: () => {
        this.comentando.set(false);
        this.comentario.set('');
        this.recargarDetalle();
      },
      error: () => { this.comentando.set(false); this.toasts.error('No se pudo comentar.'); },
    });
  }
  private recargarDetalle(): void {
    const d = this.detalle();
    if (!d) return;
    this.api.publicacion(d.anuncio.id).subscribe({ next: (nd) => this.detalle.set(nd) });
    this.cargar();
  }

  abrirMenu(cId: string, ev: Event): void {
    ev.stopPropagation();
    this.menuComentario.set(this.menuComentario() === cId ? null : cId);
  }
  empezarEdicion(cId: string, texto: string): void {
    this.menuComentario.set(null);
    this.editandoComentario.set(cId);
    this.editTexto.set(texto);
  }
  guardarEdicion(cId: string): void {
    const d = this.detalle();
    if (!d || this.editTexto().trim().length === 0) return;
    this.api.editarComentario(d.anuncio.id, cId, this.editTexto().trim()).subscribe({
      next: () => { this.editandoComentario.set(null); this.recargarDetalle(); },
      error: () => this.toasts.error('No se pudo editar.'),
    });
  }
  pedirEliminar(cId: string): void {
    this.menuComentario.set(null);
    this.confirmarEliminar.set(cId);
  }
  eliminarComentario(): void {
    const d = this.detalle(); const cId = this.confirmarEliminar();
    if (!d || !cId) return;
    this.api.eliminarComentario(d.anuncio.id, cId).subscribe({
      next: () => { this.confirmarEliminar.set(null); this.toasts.exito('Comentario eliminado'); this.recargarDetalle(); },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }
}
