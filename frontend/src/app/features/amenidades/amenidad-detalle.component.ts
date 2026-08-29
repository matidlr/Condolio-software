import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AmenidadService } from '../../core/services/amenidad.service';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { ToastService } from '../../core/services/toast.service';
import { Adjunto } from '../../core/models/consorcio.models';
import {
  Amenidad, DIAS_SEMANA, INTERVALOS_RESERVA, horaLabel,
} from '../../core/models/amenidad.models';

@Component({
  selector: 'app-amenidad-detalle',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './amenidad-detalle.component.html',
  styleUrl: './amenidades.component.scss',
})
export class AmenidadDetalleComponent {
  private router = inject(Router);
  private ruta = inject(ActivatedRoute);
  private consorcios = inject(ConsorcioService);
  private api = inject(AmenidadService);
  private adjuntos = inject(AdjuntoService);
  private toasts = inject(ToastService);

  dias = DIAS_SEMANA;
  horaLabel = horaLabel;

  amenidad = signal<Amenidad | null>(null);
  cargando = signal(true);
  imagenes = signal<string[]>([]);
  imgActiva = signal(0);
  confirmar = signal(false);
  mesCal = signal(new Date());

  documentos = signal<Adjunto[]>([]);
  docModal = signal(false);
  docPendiente = signal<File | null>(null);
  docNombre = signal('');
  subiendoDoc = signal(false);
  renombrandoId = signal<string | null>(null);
  renombreTexto = signal('');
  docAEliminar = signal<Adjunto | null>(null);

  private id = this.ruta.snapshot.paramMap.get('id')!;

  intervaloLabel = computed(() => {
    const a = this.amenidad();
    return a ? INTERVALOS_RESERVA.find((o) => o.valor === a.intervaloMinutos)?.label ?? '—' : '—';
  });

  constructor() {
    const cid = this.consorcios.activoId();
    if (cid) {
      this.api.obtener(cid, this.id).subscribe({
        next: (a) => {
          this.amenidad.set(a);
          this.cargando.set(false);
          a.imagenesIds.forEach((adjId) => this.adjuntos.descargar(adjId).subscribe((b) =>
            this.imagenes.update((l) => [...l, URL.createObjectURL(b)])));
          this.cargarDocumentos(a);
        },
        error: () => { this.toasts.error('No se pudo cargar la amenidad.'); this.cargando.set(false); },
      });
    }
  }

  private cargarDocumentos(a: Amenidad): void {
    this.adjuntos.listar('Amenidad', this.id).subscribe((lista) => {
      const imgs = new Set(a.imagenesIds);
      this.documentos.set(lista.filter((d) => !d.esImagen && !imgs.has(d.id)));
    });
  }

  // ---- documentos ----
  abrirDocModal(): void { this.docModal.set(true); this.docPendiente.set(null); this.docNombre.set(''); }
  onDocElegido(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const f = input.files?.[0];
    if (f && f.type === 'application/pdf' && f.size <= 10 * 1024 * 1024) {
      this.docPendiente.set(f);
      this.docNombre.set(f.name.replace(/\.pdf$/i, ''));
    } else if (f) {
      this.toasts.error('Solo PDF de hasta 10 MB.');
    }
    input.value = '';
  }
  subirDoc(): void {
    const f = this.docPendiente();
    if (!f || this.subiendoDoc()) return;
    this.subiendoDoc.set(true);
    const nombre = (this.docNombre().trim() || f.name.replace(/\.pdf$/i, '')) + '.pdf';
    this.adjuntos.subir('Amenidad', this.id, new File([f], nombre, { type: 'application/pdf' })).subscribe({
      next: () => {
        this.subiendoDoc.set(false);
        this.docModal.set(false);
        this.toasts.exito('Documento subido');
        const a = this.amenidad();
        if (a) this.cargarDocumentos(a);
      },
      error: (e) => { this.subiendoDoc.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo subir.'); },
    });
  }
  verDoc(d: Adjunto): void {
    this.adjuntos.descargar(d.id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    });
  }

  empezarRenombre(d: Adjunto): void {
    this.renombrandoId.set(d.id);
    this.renombreTexto.set(d.nombreArchivo.replace(/\.pdf$/i, ''));
  }
  cancelarRenombre(): void { this.renombrandoId.set(null); }
  confirmarRenombre(d: Adjunto): void {
    const nuevo = this.renombreTexto().trim();
    if (!nuevo) return;
    this.adjuntos.renombrar(d.id, nuevo).subscribe({
      next: (act) => {
        this.documentos.update((l) => l.map((x) => x.id === d.id ? act : x));
        this.renombrandoId.set(null);
        this.toasts.exito('Documento renombrado');
      },
      error: () => this.toasts.error('No se pudo renombrar.'),
    });
  }

  eliminarDoc(): void {
    const d = this.docAEliminar();
    if (!d) return;
    this.adjuntos.eliminar(d.id).subscribe({
      next: () => {
        this.documentos.update((l) => l.filter((x) => x.id !== d.id));
        this.docAEliminar.set(null);
        this.toasts.exito('Documento eliminado');
      },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }
  tamanoDoc(n: number): string { return (n / 1024 / 1024).toFixed(2) + ' MB'; }

  horario(dia: number) {
    return this.amenidad()?.horarios.find((h) => h.dia === dia);
  }

  moneda(n: number): string {
    return n.toLocaleString('es-AR', { style: 'currency', currency: 'ARS' });
  }

  volver(): void { this.router.navigate(['/panel/amenidades/directorio']); }
  editar(): void { this.router.navigate(['/panel/amenidades/nueva'], { queryParams: { editar: this.id } }); }

  eliminar(): void {
    const cid = this.consorcios.activoId();
    if (!cid) return;
    this.api.eliminar(cid, this.id).subscribe({
      next: () => {
        this.api.refrescarTotal(cid);
        this.toasts.exito('Amenidad eliminada');
        this.router.navigate(['/panel/amenidades/directorio']);
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  // ---- calendario disponibilidad ----
  grillaMes = computed(() => {
    const base = this.mesCal();
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    const arranque = new Date(primero);
    arranque.setDate(1 - primero.getDay());
    const bloqueados = new Set(this.amenidad()?.diasBloqueados ?? []);
    const desde = this.amenidad()?.reservableDesde;
    const celdas: { fecha: string; dia: number; otroMes: boolean; bloqueado: boolean; previo: boolean }[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(arranque);
      d.setDate(arranque.getDate() + i);
      const fecha = d.toISOString().slice(0, 10);
      celdas.push({
        fecha, dia: d.getDate(),
        otroMes: d.getMonth() !== base.getMonth(),
        bloqueado: bloqueados.has(fecha),
        previo: !!desde && fecha < desde,
      });
    }
    return celdas;
  });
  mesLabel = computed(() =>
    this.mesCal().toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));
  cambiarMes(delta: number): void {
    const d = new Date(this.mesCal());
    d.setMonth(d.getMonth() + delta);
    this.mesCal.set(d);
  }
}
