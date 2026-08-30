import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { MiAmenidadService, MiReserva } from '../../core/services/mi-amenidad.service';
import { PortalService } from '../../core/services/portal.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-reserva-detalle',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './portal-reserva-detalle.component.html',
  styleUrl: './portal-reserva-detalle.component.scss',
})
export class PortalReservaDetalleComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(MiAmenidadService);
  private adjuntos = inject(AdjuntoService);
  private portal = inject(PortalService);
  private toasts = inject(ToastService);

  cargando = signal(true);
  reserva = signal<MiReserva | null>(null);
  portada = signal<string | null>(null);
  confirmar = signal(false);
  eliminando = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.reserva(id).subscribe({
      next: (r) => {
        this.reserva.set(r);
        this.cargando.set(false);
        const img = r.imagenesIds?.[0];
        if (img) this.adjuntos.descargar(img).subscribe({ next: (b) => this.portada.set(URL.createObjectURL(b)), error: () => {} });
      },
      error: () => { this.toasts.error('No pudimos abrir la reserva.'); this.cargando.set(false); },
    });
  }

  cerrar(): void { this.router.navigate(['/portal/amenidades']); }

  estadoMeta(e: string | undefined): { label: string; color: string } {
    switch (e) {
      case 'Confirmada': return { label: 'Confirmada', color: '#16a34a' };
      case 'Pendiente': return { label: 'Pendiente', color: '#d97706' };
      case 'Rechazada': return { label: 'Rechazada', color: '#dc2626' };
      default: return { label: 'Cancelada', color: '#64748b' };
    }
  }

  diaRelativo(iso: string): string {
    const d = new Date(iso); const hoy = new Date();
    const dif = Math.round((new Date(d).setHours(0, 0, 0, 0) - hoy.setHours(0, 0, 0, 0)) / 86_400_000);
    if (dif === 0) return 'Hoy';
    if (dif === 1) return 'Mañana';
    if (dif === -1) return 'Ayer';
    if (dif > 1 && dif < 7) return new Date(iso).toLocaleDateString('es-AR', { weekday: 'long' });
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  eliminar(): void {
    const r = this.reserva();
    if (!r || this.eliminando()) return;
    this.eliminando.set(true);
    this.api.cancelar(r.id).subscribe({
      next: () => {
        this.toasts.exito('Reserva eliminada');
        this.portal.cargarCasa().subscribe();
        this.router.navigate(['/portal/amenidades']);
      },
      error: () => { this.eliminando.set(false); this.toasts.error('No se pudo eliminar.'); },
    });
  }
}
