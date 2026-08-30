import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { PortalService } from '../../core/services/portal.service';

@Component({
  selector: 'app-portal-inicio',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './portal-inicio.component.html',
  styleUrl: './portal-inicio.component.scss',
})
export class PortalInicioComponent {
  private auth = inject(AuthService);
  portal = inject(PortalService);

  cargando = signal(true);
  nombrePila = computed(() => (this.auth.nombre() || '').split(' ')[0]);

  constructor() {
    this.portal.cargarPanel().subscribe({
      next: () => this.cargando.set(false),
      error: () => this.cargando.set(false),
    });
  }

  money(n: number | null | undefined): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  saldoTono(n: number): 'ok' | 'deuda' {
    return n > 0 ? 'deuda' : 'ok';
  }
}
