import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MiNotificacionService, NotifResidente, META_TIPO_NOTIF } from '../../core/services/mi-notificacion.service';
import { PortalService } from '../../core/services/portal.service';
import { ToastService } from '../../core/services/toast.service';

type MetaTipoLocal = { icon: string; color: string };

@Component({
  selector: 'app-portal-notificaciones',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './portal-notificaciones.component.html',
  styleUrl: './portal-notificaciones.component.scss',
})
export class PortalNotificacionesComponent {
  private api = inject(MiNotificacionService);
  private portal = inject(PortalService);
  private router = inject(Router);
  private toasts = inject(ToastService);

  cargando = signal(true);
  items = signal<NotifResidente[]>([]);
  filtro = signal<'todas' | 'noleidas'>('todas');

  visibles = computed(() =>
    this.filtro() === 'noleidas' ? this.items().filter((n) => !n.leida) : this.items());

  noLeidas = computed(() => this.items().filter((n) => !n.leida).length);

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.listar().subscribe({
      next: (l) => {
        this.items.set(l.notificaciones);
        this.portal.notifNoLeidas.set(l.noLeidas);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las notificaciones.'); this.cargando.set(false); },
    });
  }

  meta(tipo: string): MetaTipoLocal {
    return META_TIPO_NOTIF[tipo] ?? META_TIPO_NOTIF['General'];
  }

  hace(iso: string): string {
    const s = Math.max(1, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
    if (s < 60) return 'recién';
    if (s < 3600) return `hace ${Math.floor(s / 60)} min`;
    if (s < 86400) return `hace ${Math.floor(s / 3600)} h`;
    if (s < 604800) return `hace ${Math.floor(s / 86400)} d`;
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
  }

  abrir(n: NotifResidente): void {
    if (!n.leida) {
      this.api.marcarLeida(n.id).subscribe(() => {
        this.items.update((l) => l.map((x) => x.id === n.id ? { ...x, leida: true } : x));
        this.portal.notifNoLeidas.update((v) => Math.max(0, v - 1));
      });
    }
    if (n.enlace) this.router.navigateByUrl(n.enlace);
  }

  marcarTodas(): void {
    if (this.noLeidas() === 0) return;
    this.api.marcarTodasLeidas().subscribe({
      next: () => {
        this.items.update((l) => l.map((x) => ({ ...x, leida: true })));
        this.portal.notifNoLeidas.set(0);
      },
      error: () => this.toasts.error('No se pudo actualizar.'),
    });
  }
}
