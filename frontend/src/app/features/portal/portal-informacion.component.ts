import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ComunidadInfo, PortalService } from '../../core/services/portal.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-informacion',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './portal-informacion.component.html',
  styleUrl: './portal-informacion.component.scss',
})
export class PortalInformacionComponent {
  private api = inject(PortalService);
  private toasts = inject(ToastService);

  vista = signal<'lista' | 'direccion'>('lista');
  cargando = signal(true);
  info = signal<ComunidadInfo | null>(null);

  inicial = computed(() => (this.info()?.nombre ?? '?').trim().charAt(0).toUpperCase());

  direccionResumen = computed(() => {
    const i = this.info();
    if (!i) return '';
    return [i.direccion, i.localidad, i.provincia].filter(Boolean).join(', ') || 'Sin definir';
  });

  paisLabel = computed(() => {
    const p = this.info()?.pais;
    if (!p) return 'Sin definir';
    return p.toUpperCase() === 'AR' ? 'Argentina' : p;
  });

  constructor() {
    this.api.comunidad().subscribe({
      next: (r) => { this.info.set(r); this.cargando.set(false); },
      error: (e) => {
        this.cargando.set(false);
        this.toasts.error(e?.error?.message ?? 'No pudimos cargar la información.');
      },
    });
  }
}
