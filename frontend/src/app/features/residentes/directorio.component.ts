import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ResidenteService } from '../../core/services/residente.service';
import { Directorio, Residente } from '../../core/models/residente.models';
import { InvitarResidenteComponent } from './invitar-residente.component';
import { AyudaPanelComponent } from '../../shared/ayuda-panel.component';

type Filtro = 'Todos' | 'Propietario' | 'Inquilino' | 'Gestor';

@Component({
  selector: 'app-directorio',
  standalone: true,
  imports: [InvitarResidenteComponent, AyudaPanelComponent],
  templateUrl: './directorio.component.html',
  styleUrl: './directorio.component.scss',
})
export class DirectorioComponent {
  private router = inject(Router);
  consorcios = inject(ConsorcioService);
  private api = inject(ResidenteService);

  data = signal<Directorio | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);
  busqueda = signal('');
  filtro = signal<Filtro>('Todos');
  invitarAbierto = signal(false);
  ayuda = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const f = this.filtro();
    return (this.data()?.residentes ?? [])
      .filter((r) => f === 'Todos' || r.rol === f)
      .filter((r) => !q
        || `${r.nombre} ${r.apellido}`.toLowerCase().includes(q)
        || (r.email ?? '').toLowerCase().includes(q)
        || r.unidadNombre.toLowerCase().includes(q));
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.error.set(null);
    this.api.directorio(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.error.set('No pudimos cargar el directorio.'); this.cargando.set(false); },
    });
  }

  refrescar(): void {
    const id = this.consorcioId();
    if (id) this.cargar(id);
  }

  cerrar(): void {
    this.router.navigate(['/panel/unidades']);
  }

  onInvitado(): void {
    this.invitarAbierto.set(false);
    this.refrescar();
  }

  iniciales(r: Residente): string {
    return ((r.nombre[0] ?? '') + (r.apellido[0] ?? '')).toUpperCase() || '?';
  }
}
