import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MiUnidadDetalle, MiUnidadPersona, PortalService } from '../../core/services/portal.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-mi-unidad',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="mu">
      @if (cargando()) {
        <p class="mu-loading">Cargando…</p>
      } @else if (u()) {
        @if (u()!; as d) {
        <div class="mu-header">
          <span class="mu-header__ph">{{ d.unidadNombre.trim().charAt(0).toUpperCase() }}</span>
          <div>
            <h1>Unidad {{ d.unidadNombre }}</h1>
            <p>{{ d.consorcioNombre }}</p>
          </div>
        </div>

        <h2 class="mu-secttl">Información de la unidad</h2>
        <article class="mu-card mu-card--grid">
          <div><small>Unidad</small><b>{{ d.unidadNombre }}</b></div>
          <div><small>Piso</small><b>{{ d.piso }}</b></div>
          <div><small>Tipo</small><b>{{ d.tipoUnidad }}</b></div>
          <div><small>Ocupación</small><b>{{ d.ocupacion }}</b></div>
          <div><small>Total de residentes</small><b>{{ d.totalResidentes }}</b></div>
        </article>

        <h2 class="mu-secttl">Propietarios ({{ d.propietarios.length }})</h2>
        <article class="mu-card">
          @for (p of d.propietarios; track p.nombre) {
            <div class="mu-person">
              <span class="mu-person__av">{{ inicial(p) }}</span>
              <div class="mu-person__body">
                <b>{{ p.nombre || 'Sin nombre' }}</b>
                <small>Propietario</small>
              </div>
              @if (p.esContactoPrincipal) { <span class="mu-person__tag">Contacto principal</span> }
            </div>
          } @empty {
            <p class="mu-empty">Todavía no hay propietarios cargados.</p>
          }
        </article>

        @if (d.inquilinos.length) {
          <h2 class="mu-secttl">Inquilinos ({{ d.inquilinos.length }})</h2>
          <article class="mu-card">
            @for (p of d.inquilinos; track p.nombre) {
              <div class="mu-person">
                <span class="mu-person__av mu-person__av--alt">{{ inicial(p) }}</span>
                <div class="mu-person__body">
                  <b>{{ p.nombre || 'Sin nombre' }}</b>
                  <small>Inquilino</small>
                </div>
                @if (p.esContactoPrincipal) { <span class="mu-person__tag">Contacto principal</span> }
              </div>
            }
          </article>
        }
        }
      } @else {
        <div class="mu-card mu-empty-card">
          <h2>Todavía no tenés una unidad asignada</h2>
          <p>Cuando la administración te asigne una unidad, vas a verla acá.</p>
          <a class="btn btn--ghost" routerLink="/portal/casa">Volver al inicio</a>
        </div>
      }
    </section>
  `,
  styles: [`
    :host { display: block; }
    .mu-loading { color: var(--c-text-muted); padding: 20px 0; }

    .mu-header {
      display: flex; align-items: center; gap: 14px;
      background: var(--c-surface); border: 1px solid var(--c-border); border-radius: 16px; padding: 16px 18px;
      margin-bottom: 6px;
    }
    .mu-header__ph {
      width: 56px; height: 56px; border-radius: 14px; flex-shrink: 0;
      background: linear-gradient(140deg, #3a5568, #17263a); color: #fff;
      display: grid; place-items: center; font-size: 1.4rem; font-weight: 700;
    }
    .mu-header h1 { margin: 0; font-size: 1.3rem; }
    .mu-header p { margin: 3px 0 0; color: var(--c-text-muted); font-size: 0.86rem; }

    .mu-secttl {
      margin: 18px 0 8px; font-size: 0.78rem; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.06em; color: var(--c-text-soft);
    }

    .mu-card {
      background: var(--c-surface); border: 1px solid var(--c-border); border-radius: 16px;
      overflow: hidden;
    }
    .mu-card--grid {
      display: grid; grid-template-columns: repeat(2, 1fr); gap: 1px;
      background: var(--c-border); padding: 1px;
    }
    .mu-card--grid > div {
      background: var(--c-surface); padding: 14px 16px;
      display: flex; flex-direction: column; gap: 3px;
    }
    .mu-card--grid small { color: var(--c-text-muted); font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .mu-card--grid b { font-size: 0.98rem; }

    .mu-person {
      display: flex; align-items: center; gap: 12px; padding: 13px 16px;
      border-top: 1px solid var(--c-border);
    }
    .mu-person:first-child { border-top: 0; }
    .mu-person__av {
      width: 40px; height: 40px; border-radius: 50%; flex-shrink: 0;
      display: grid; place-items: center; font-weight: 700; font-size: 0.95rem;
      background: linear-gradient(140deg, #3b82f6, #1d4ed8); color: #fff;
    }
    .mu-person__av--alt { background: linear-gradient(140deg, #a855f7, #7c3aed); }
    .mu-person__body { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 1px; }
    .mu-person__body b { font-size: 0.94rem; }
    .mu-person__body small { color: var(--c-text-muted); font-size: 0.78rem; }
    .mu-person__tag {
      font-size: 0.7rem; font-weight: 600; padding: 3px 9px; border-radius: 999px; flex-shrink: 0;
      background: color-mix(in srgb, var(--c-success) 15%, transparent); color: var(--c-success);
    }

    .mu-empty { padding: 20px 16px; text-align: center; color: var(--c-text-soft); font-size: 0.88rem; }
    .mu-empty-card { padding: 28px 20px; text-align: center; }
    .mu-empty-card h2 { margin: 0 0 6px; font-size: 1.05rem; }
    .mu-empty-card p { margin: 0 0 16px; color: var(--c-text-muted); font-size: 0.9rem; }

    @media (min-width: 600px) {
      .mu-card--grid { grid-template-columns: repeat(3, 1fr); }
    }
  `],
})
export class MiUnidadComponent {
  private api = inject(PortalService);
  private toasts = inject(ToastService);

  cargando = signal(true);
  u = signal<MiUnidadDetalle | null>(null);

  constructor() {
    this.api.unidad().subscribe({
      next: (r) => { this.u.set(r); this.cargando.set(false); },
      error: (e) => {
        this.cargando.set(false);
        this.toasts.error(e?.error?.message ?? 'No pudimos cargar tu unidad.');
      },
    });
  }

  inicial(p: MiUnidadPersona): string {
    return (p.nombre || '?').trim().charAt(0).toUpperCase();
  }
}
