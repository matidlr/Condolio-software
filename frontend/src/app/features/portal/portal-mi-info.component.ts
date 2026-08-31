import { Component, computed, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { ComunidadInfo, PortalService } from '../../core/services/portal.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-portal-mi-info',
  standalone: true,
  template: `
    <section class="pmi">
      <header class="pmi-top">
        <button type="button" class="pmi-back" (click)="volver()">‹ Atrás</button>
        <h1>Mi información</h1>
      </header>

      <article class="pmi-card">
        <div class="pmi-row"><small>Nombre</small><b>{{ nombre() }}</b></div>
        <div class="pmi-row"><small>Apellido</small><b>{{ apellido() }}</b></div>
        <div class="pmi-row"><small>Correo</small><b>{{ email() }}</b></div>
        @if (info(); as i) {
          <div class="pmi-row"><small>Unidad</small><b>{{ i.unidadNombre }} · {{ i.rolEnUnidad }}</b></div>
          <div class="pmi-row"><small>Comunidad</small><b>{{ i.nombre }}</b></div>
        }
      </article>

      <p class="pmi-nota">
        Para cambiar tu nombre, teléfono o unidad, comunicate con la administración de tu comunidad.
      </p>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .pmi-top { display: flex; align-items: center; gap: 10px; margin-bottom: 16px; }
    .pmi-back { border: none; background: none; color: var(--c-text-muted); font: inherit; cursor: pointer; padding: 4px 0; }
    .pmi-back:hover { color: var(--c-text); }
    .pmi-top h1 { margin: 0; font-size: 1.35rem; }
    .pmi-card { background: var(--c-surface); border: 1px solid var(--c-border); border-radius: 16px; overflow: hidden; }
    .pmi-row { padding: 13px 16px; border-top: 1px solid var(--c-border); display: flex; flex-direction: column; gap: 2px; }
    .pmi-row:first-child { border-top: 0; }
    .pmi-row small { color: var(--c-text-muted); font-size: 0.76rem; }
    .pmi-row b { font-size: 0.95rem; }
    .pmi-nota { margin: 16px 2px 0; font-size: 0.82rem; color: var(--c-text-soft); line-height: 1.5; }
  `],
})
export class PortalMiInfoComponent {
  private auth = inject(AuthService);
  private portal = inject(PortalService);
  private location = inject(Location);

  info = signal<ComunidadInfo | null>(null);

  private partes = computed(() => (this.auth.nombre() || '').trim().split(/\s+/));
  nombre = computed(() => this.partes()[0] || '—');
  apellido = computed(() => this.partes().slice(1).join(' ') || '—');
  email = computed(() => this.auth.sesion()?.email ?? '—');

  constructor() {
    this.portal.comunidad().subscribe({ next: (i) => this.info.set(i), error: () => {} });
  }

  volver(): void {
    this.location.back();
  }
}
