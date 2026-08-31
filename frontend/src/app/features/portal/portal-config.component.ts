import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface FilaConfig {
  label: string;
  sub: string;
  icon: string;
  ruta?: string;
  href?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-portal-config',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="pcf">
      <div class="pcf-profile">
        <span class="pcf-avatar">{{ iniciales() }}</span>
        <b>{{ auth.nombre() || 'Residente' }}</b>
        <span class="pcf-mail">{{ email() }}</span>
      </div>

      @for (grupo of grupos; track grupo.titulo) {
        <h2 class="pcf-secttl">{{ grupo.titulo }}</h2>
        <div class="pcf-card">
          @for (f of grupo.filas; track f.label) {
            @if (f.ruta) {
              <a class="pcf-row" [routerLink]="f.ruta" [class.is-danger]="f.danger">
                <span class="pcf-row__ic">{{ f.icon }}</span>
                <span class="pcf-row__body"><b>{{ f.label }}</b><small>{{ f.sub }}</small></span>
                <span class="pcf-row__go">›</span>
              </a>
            } @else if (f.href) {
              <a class="pcf-row" [href]="f.href" target="_blank" rel="noopener">
                <span class="pcf-row__ic">{{ f.icon }}</span>
                <span class="pcf-row__body"><b>{{ f.label }}</b><small>{{ f.sub }}</small></span>
                <span class="pcf-row__go">›</span>
              </a>
            } @else {
              <div class="pcf-row pcf-row--static">
                <span class="pcf-row__ic">{{ f.icon }}</span>
                <span class="pcf-row__body"><b>{{ f.label }}</b><small>{{ f.sub }}</small></span>
              </div>
            }
          }
        </div>
      }

      <button type="button" class="pcf-logout" (click)="salir()">⎋ Cerrar sesión</button>
    </section>
  `,
  styleUrl: './portal-config.component.scss',
})
export class PortalConfigComponent {
  auth = inject(AuthService);

  email = computed(() => this.auth.sesion()?.email ?? '');
  iniciales = computed(() =>
    (this.auth.nombre() || 'R').split(/\s+/).map((p) => p[0] ?? '').slice(0, 2).join('').toUpperCase());

  readonly grupos: { titulo: string; filas: FilaConfig[] }[] = [
    {
      titulo: 'Cuenta',
      filas: [
        { label: 'Mi información', sub: 'Nombre, email, teléfono', icon: '👤', ruta: '/portal/config/mi-info' },
        { label: 'Idioma', sub: 'Español', icon: '🌐' },
      ],
    },
    {
      titulo: 'Seguridad',
      filas: [
        { label: 'Contraseña', sub: 'Cambiá tu contraseña', icon: '🔒', ruta: '/portal/config/seguridad' },
      ],
    },
    {
      titulo: 'Preferencias',
      filas: [
        { label: 'Notificaciones', sub: 'Configurar alertas', icon: '🔔', ruta: '/portal/config/notificaciones' },
      ],
    },
    {
      titulo: 'Legal',
      filas: [
        { label: 'Términos de servicio', sub: 'Términos y condiciones', icon: '📄', href: '/terminos' },
        { label: 'Política de privacidad', sub: 'Cómo manejamos tus datos', icon: '🛡', href: '/privacidad' },
      ],
    },
  ];

  salir(): void {
    this.auth.logout();
    location.href = '/login';
  }
}
