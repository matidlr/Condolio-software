import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MiEncuestaService } from '../../core/services/mi-encuesta.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Encuesta, EncuestaDetalle } from '../../core/models/encuesta.models';

@Component({
  selector: 'app-portal-encuestas',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './portal-encuestas.component.html',
  styleUrl: './portal-encuestas.component.scss',
})
export class PortalEncuestasComponent {
  private api = inject(MiEncuestaService);
  private auth = inject(AuthService);
  private toasts = inject(ToastService);

  miNombre = this.auth.nombre;

  vista = signal<'lista' | 'detalle'>('lista');
  cargando = signal(true);
  encuestas = signal<Encuesta[]>([]);
  busqueda = signal('');
  tab = signal<'curso' | 'terminadas'>('curso');

  detalle = signal<EncuestaDetalle | null>(null);
  seleccion = signal<string[]>([]);
  votando = signal(false);
  revotar = signal(false);
  confirmarCambio = signal(false);

  filtradas = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const t = this.tab();
    return this.encuestas().filter((e) =>
      (t === 'curso' ? e.estado === 'Activa' : e.estado === 'Cerrada') &&
      (!q || e.titulo.toLowerCase().includes(q)));
  });

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.listar().subscribe({
      next: (l) => {
        this.encuestas.set(l);
        this.api.pendientes.set(l.filter((e) => e.estado === 'Activa' && !e.yoVote).length);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las encuestas.'); this.cargando.set(false); },
    });
  }

  textoCierre(e: { cierreUtc?: string | null }): string {
    if (!e.cierreUtc) return 'Sin fecha de cierre';
    return 'Termina ' + new Date(e.cierreUtc).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  abrir(e: Encuesta): void {
    this.detalle.set(null);
    this.seleccion.set([]);
    this.revotar.set(false);
    this.vista.set('detalle');
    this.api.obtener(e.id).subscribe({
      next: (d) => this.detalle.set(d),
      error: () => { this.toasts.error('No se pudo abrir la encuesta.'); this.vista.set('lista'); },
    });
  }

  mostrarResultados = computed(() => {
    const d = this.detalle();
    return !!d && (d.encuesta.yoVote || d.encuesta.estado === 'Cerrada') && !this.revotar();
  });

  toggleOpcion(opcionId: string): void {
    const d = this.detalle();
    if (!d) return;
    this.seleccion.update((s) => {
      if (d.encuesta.multiplesOpciones) {
        return s.includes(opcionId) ? s.filter((x) => x !== opcionId) : [...s, opcionId];
      }
      return s.includes(opcionId) ? [] : [opcionId];
    });
  }
  elegida(id: string): boolean { return this.seleccion().includes(id); }
  puedeVotar = computed(() => this.seleccion().length > 0);

  votar(): void {
    const d = this.detalle();
    if (!d || !this.puedeVotar() || this.votando()) return;
    this.votando.set(true);
    this.api.votar(d.encuesta.id, this.seleccion()).subscribe({
      next: (act) => {
        this.votando.set(false);
        this.revotar.set(false);
        this.detalle.update((cur) => cur ? { ...cur, encuesta: act } : cur);
        this.encuestas.update((l) => l.map((x) => x.id === act.id ? act : x));
        this.api.pendientes.update((n) => Math.max(0, n - 1));
        this.toasts.exito('¡Tu voto ha sido registrado!');
      },
      error: (e) => { this.votando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo votar.'); },
    });
  }

  cambiarVoto(): void {
    this.confirmarCambio.set(false);
    this.seleccion.set([]);
    this.revotar.set(true);
  }
}
