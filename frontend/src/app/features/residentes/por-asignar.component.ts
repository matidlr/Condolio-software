import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ResidenteService } from '../../core/services/residente.service';
import { ResidenteSinUnidad } from '../../core/models/residente.models';
import { AyudaPanelComponent } from '../../shared/ayuda-panel.component';

@Component({
  selector: 'app-por-asignar',
  standalone: true,
  imports: [AyudaPanelComponent],
  templateUrl: './por-asignar.component.html',
  styleUrl: './directorio.component.scss',
})
export class PorAsignarComponent {
  private router = inject(Router);
  consorcios = inject(ConsorcioService);
  private api = inject(ResidenteService);

  residentes = signal<ResidenteSinUnidad[]>([]);
  cargando = signal(true);
  busqueda = signal('');
  ayuda = signal(false);

  private cid = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return this.residentes().filter((r) => !q
      || `${r.nombre} ${r.apellido}`.toLowerCase().includes(q)
      || r.email.toLowerCase().includes(q));
  });

  constructor() {
    effect(() => { const id = this.cid(); if (id) this.cargar(id); });
  }

  private cargar(id: string): void {
    this.cargando.set(true);
    this.api.porAsignar(id).subscribe({
      next: (l) => { this.residentes.set(l); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }

  refrescar(): void { const id = this.cid(); if (id) this.cargar(id); }
  volver(): void { this.router.navigate(['/panel/unidades']); }

  iniciales(r: ResidenteSinUnidad): string {
    return ((r.nombre[0] ?? '') + (r.apellido[0] ?? '')).toUpperCase() || '?';
  }
}
