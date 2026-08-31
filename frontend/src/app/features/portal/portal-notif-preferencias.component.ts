import { Component, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { CATEGORIAS_NOTIF, MiNotificacionService, PreferenciasNotif } from '../../core/services/mi-notificacion.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-notif-preferencias',
  standalone: true,
  template: `
    <section class="pnp">
      <header class="pnp-top">
        <button type="button" class="pnp-back" (click)="volver()">‹ Atrás</button>
        <h1>Notificaciones</h1>
      </header>
      <p class="pnp-lead">
        Elegí sobre qué querés recibir avisos. Las de la app aparecen en tu campana;
        las de correo llegan a tu email.
      </p>

      @if (cargando()) {
        <p class="pnp-loading">Cargando…</p>
      } @else {
        @for (c of categorias; track c.key) {
          <article class="pnp-card">
            <div class="pnp-card__head">
              <span class="pnp-card__ic" [style.background]="c.color + '22'" [style.color]="c.color">{{ c.icon }}</span>
              <div><b>{{ c.titulo }}</b><small>{{ c.desc }}</small></div>
            </div>
            <div class="pnp-toggles">
              <label class="pnp-tg">
                <span>App</span>
                <input type="checkbox" [checked]="valor(c.key, 'App')"
                  (change)="set(c.key, 'App', $any($event.target).checked)" />
                <span class="pnp-sw"></span>
              </label>
              <label class="pnp-tg">
                <span>Correo</span>
                <input type="checkbox" [checked]="valor(c.key, 'Mail')"
                  (change)="set(c.key, 'Mail', $any($event.target).checked)" />
                <span class="pnp-sw"></span>
              </label>
            </div>
          </article>
        }
        <p class="pnp-nota">✉️ El envío por correo todavía no está disponible; la preferencia queda guardada para cuando lo esté.</p>
      }
    </section>
  `,
  styleUrl: './portal-notif-preferencias.component.scss',
})
export class PortalNotifPreferenciasComponent {
  private api = inject(MiNotificacionService);
  private toasts = inject(ToastService);
  private location = inject(Location);

  categorias = CATEGORIAS_NOTIF;
  cargando = signal(true);
  prefs = signal<PreferenciasNotif | null>(null);

  constructor() {
    this.api.preferencias().subscribe({
      next: (p) => { this.prefs.set(p); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar tus preferencias.'); this.cargando.set(false); },
    });
  }

  private campo(key: string, canal: 'App' | 'Mail'): keyof PreferenciasNotif {
    return (key + canal) as keyof PreferenciasNotif;
  }

  valor(key: string, canal: 'App' | 'Mail'): boolean {
    const p = this.prefs();
    return p ? !!p[this.campo(key, canal)] : false;
  }

  set(key: string, canal: 'App' | 'Mail', v: boolean): void {
    const p = this.prefs();
    if (!p) return;
    const actualizado = { ...p, [this.campo(key, canal)]: v };
    this.prefs.set(actualizado);
    this.api.guardarPreferencias(actualizado).subscribe({
      error: () => { this.toasts.error('No se pudo guardar.'); this.prefs.set(p); },
    });
  }

  volver(): void {
    this.location.back();
  }
}
