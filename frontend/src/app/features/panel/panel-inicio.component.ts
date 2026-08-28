import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-panel-inicio',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="page">
      <header class="page__head">
        <h1>Hola, {{ nombre() }}</h1>
        <p>Resumen de tu administración</p>
      </header>

      @if (consorcios.consorcios().length === 0) {
        <div class="card card--empty">
          <h2>Creá tu primer consorcio</h2>
          <p>Para empezar a cargar unidades necesitás dar de alta un consorcio.</p>
          <a class="btn btn--primary" routerLink="/panel/unidades">Ir a Unidades</a>
        </div>
      } @else {
        <div class="grid">
          <div class="card">
            <span class="card__label">Consorcios</span>
            <strong class="card__value">{{ consorcios.consorcios().length }}</strong>
          </div>
          <div class="card">
            <span class="card__label">Unidades totales</span>
            <strong class="card__value">{{ totalUnidades() }}</strong>
          </div>
          <div class="card">
            <span class="card__label">Consorcio activo</span>
            <strong class="card__value card__value--sm">{{ consorcios.activo?.nombre ?? '—' }}</strong>
          </div>
        </div>
      }
    </section>
  `,
  styles: [`
    .page__head h1 { margin: 0 0 4px; font-size: 1.5rem; }
    .page__head p { margin: 0 0 24px; color: var(--c-text-muted); }
    .grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
    .card {
      background: var(--c-surface); border: 1px solid var(--c-border);
      border-radius: var(--radius); padding: 20px;
    }
    .card__label { font-size: 0.8rem; color: var(--c-text-soft); text-transform: uppercase; letter-spacing: 0.06em; }
    .card__value { display: block; margin-top: 8px; font-size: 1.9rem; letter-spacing: -0.02em; }
    .card__value--sm { font-size: 1.1rem; }
    .card--empty { text-align: center; padding: 44px; }
    .card--empty h2 { margin: 0 0 8px; }
    .card--empty p { margin: 0 0 18px; color: var(--c-text-muted); }
    .card--empty .btn { width: auto; padding: 0 22px; }
    @media (max-width: 820px) { .grid { grid-template-columns: 1fr; } }
  `],
})
export class PanelInicioComponent {
  consorcios = inject(ConsorcioService);
  private auth = inject(AuthService);
  nombre = this.auth.nombre;
  totalUnidades = computed(() =>
    this.consorcios.consorcios().reduce((n, c) => n + c.cantidadUnidades, 0),
  );
}
