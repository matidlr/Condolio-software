import { Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdjuntoService } from '../core/services/adjunto.service';
import { Adjunto, AdjuntoOwner } from '../core/models/consorcio.models';

const MAX = 5;

@Component({
  selector: 'app-adjuntos',
  standalone: true,
  templateUrl: './adjuntos.component.html',
  styleUrl: './adjuntos.component.scss',
})
export class AdjuntosComponent {
  ownerTipo = input.required<AdjuntoOwner>();
  ownerId = input.required<string>();
  editable = input(true);

  private api = inject(AdjuntoService);
  private destroyRef = inject(DestroyRef);

  readonly max = MAX;
  adjuntos = signal<Adjunto[]>([]);
  urls = signal<Record<string, string>>({});
  subiendo = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  ngOnDestroy(): void {
    Object.values(this.urls()).forEach((u) => URL.revokeObjectURL(u));
  }

  private cargar(): void {
    this.api.listar(this.ownerTipo(), this.ownerId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((list) => {
        this.adjuntos.set(list);
        list.filter((a) => a.esImagen && !this.urls()[a.id]).forEach((a) => this.cargarImagen(a));
      });
  }

  private cargarImagen(a: Adjunto): void {
    this.api.descargar(a.id).subscribe((blob) => {
      this.urls.update((m) => ({ ...m, [a.id]: URL.createObjectURL(blob) }));
    });
  }

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    this.subirVarios(files);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    if (!this.editable()) return;
    this.subirVarios(Array.from(event.dataTransfer?.files ?? []));
  }

  private subirVarios(files: File[]): void {
    const espacio = MAX - this.adjuntos().length;
    if (espacio <= 0) {
      this.error.set(`Máximo ${MAX} adjuntos.`);
      return;
    }
    const validos = files
      .filter((f) => /^image\/(png|jpe?g|webp|gif)$|^application\/pdf$/.test(f.type))
      .slice(0, espacio);
    if (files.length && !validos.length) {
      this.error.set('Solo se permiten imágenes o PDF.');
      return;
    }
    this.error.set(null);
    validos.forEach((f) => this.subirUno(f));
  }

  private subirUno(file: File): void {
    this.subiendo.set(true);
    this.api.subir(this.ownerTipo(), this.ownerId(), file).subscribe({
      next: (a) => {
        this.adjuntos.update((l) => [...l, a]);
        if (a.esImagen) this.cargarImagen(a);
        this.subiendo.set(false);
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'No se pudo subir el archivo.');
        this.subiendo.set(false);
      },
    });
  }

  abrir(a: Adjunto): void {
    this.api.descargar(a.id).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    });
  }

  eliminar(a: Adjunto): void {
    if (!confirm(`¿Eliminar "${a.nombreArchivo}"?`)) return;
    this.api.eliminar(a.id).subscribe(() => {
      this.adjuntos.update((l) => l.filter((x) => x.id !== a.id));
      const u = this.urls()[a.id];
      if (u) {
        URL.revokeObjectURL(u);
        this.urls.update((m) => {
          const { [a.id]: _, ...rest } = m;
          return rest;
        });
      }
    });
  }
}
