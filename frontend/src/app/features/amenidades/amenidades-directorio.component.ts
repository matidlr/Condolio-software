import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AmenidadService } from '../../core/services/amenidad.service';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { ToastService } from '../../core/services/toast.service';
import { Amenidad, AmenidadLista } from '../../core/models/amenidad.models';

type Filtro = 'todas' | 'reservables' | 'no';

@Component({
  selector: 'app-amenidades-directorio',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './amenidades-directorio.component.html',
  styleUrl: './amenidades.component.scss',
})
export class AmenidadesDirectorioComponent {
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(AmenidadService);
  private adjuntos = inject(AdjuntoService);
  private toasts = inject(ToastService);

  data = signal<AmenidadLista | null>(null);
  cargando = signal(true);
  busqueda = signal('');
  filtro = signal<Filtro>('todas');
  private portadas = signal<Record<string, string>>({});

  private consorcioId = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const f = this.filtro();
    return (this.data()?.amenidades ?? [])
      .filter((a) => f === 'todas' || (f === 'reservables' ? a.reservable : !a.reservable))
      .filter((a) => !q || a.nombre.toLowerCase().includes(q)
        || (a.descripcion ?? '').toLowerCase().includes(q));
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.listar(cid).subscribe({
      next: (d) => {
        this.data.set(d);
        this.api.total.set(d.total);
        this.cargando.set(false);
        for (const a of d.amenidades) this.cargarPortada(a);
      },
      error: () => { this.toasts.error('No pudimos cargar las amenidades.'); this.cargando.set(false); },
    });
  }

  private cargarPortada(a: Amenidad): void {
    const id = a.imagenesIds[0];
    if (!id || this.portadas()[a.id]) return;
    this.adjuntos.descargar(id).subscribe({
      next: (blob) => this.portadas.update((p) => ({ ...p, [a.id]: URL.createObjectURL(blob) })),
      error: () => {},
    });
  }

  portada(a: Amenidad): string | null {
    return this.portadas()[a.id] ?? null;
  }

  nueva(): void {
    this.router.navigate(['/panel/amenidades/nueva']);
  }

  abrir(a: Amenidad): void {
    this.router.navigate(['/panel/amenidades', a.id]);
  }

  moneda(n: number): string {
    return n.toLocaleString('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 });
  }
}
