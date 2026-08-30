import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CalendarioItem, CATEGORIAS_EVENTO, CrearEvento, MiCalendarioService } from '../../core/services/mi-calendario.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-calendario',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-calendario.component.html',
  styleUrl: './portal-calendario.component.scss',
})
export class PortalCalendarioComponent {
  private api = inject(MiCalendarioService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  categorias = CATEGORIAS_EVENTO;

  vista = signal<'mes' | 'nuevo' | 'evento'>('mes');
  cargando = signal(true);
  items = signal<CalendarioItem[]>([]);
  mes = signal(new Date());
  diaSel = signal<string>(new Date().toISOString().slice(0, 10));
  eventoSel = signal<CalendarioItem | null>(null);

  form = signal<CrearEvento>(this.formVacio());
  guardando = signal(false);

  private formVacio(): CrearEvento {
    const hoy = new Date();
    const d = hoy.toISOString().slice(0, 10);
    return {
      titulo: '', descripcion: '', ubicacion: '', categoria: 'General',
      inicio: d + 'T11:00', fin: d + 'T12:00', todoElDia: false,
    };
  }

  constructor() {
    this.cargarMes();
  }

  private cargarMes(): void {
    const m = this.mes();
    const desde = new Date(m.getFullYear(), m.getMonth() - 1, 1).toISOString();
    const hasta = new Date(m.getFullYear(), m.getMonth() + 2, 0).toISOString();
    this.cargando.set(true);
    this.api.items(desde, hasta).subscribe({
      next: (l) => { this.items.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar el calendario.'); this.cargando.set(false); },
    });
  }

  mesLabel = computed(() => {
    const m = this.mes();
    return {
      mes: m.toLocaleDateString('es-AR', { month: 'long' }),
      anio: m.getFullYear(),
    };
  });

  grid = computed(() => {
    const base = this.mes();
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    // Lunes = 0
    const offset = (primero.getDay() + 6) % 7;
    const start = new Date(primero);
    start.setDate(start.getDate() - offset);
    const dias: { iso: string; enMes: boolean; hoy: boolean; conItems: boolean }[] = [];
    const hoyIso = new Date().toISOString().slice(0, 10);
    for (let i = 0; i < 42; i++) {
      const d = new Date(start);
      d.setDate(start.getDate() + i);
      const iso = d.toISOString().slice(0, 10);
      dias.push({
        iso,
        enMes: d.getMonth() === base.getMonth(),
        hoy: iso === hoyIso,
        conItems: this.items().some((it) => it.inicio.slice(0, 10) === iso),
      });
      if (i >= 34 && d.getMonth() !== base.getMonth() && d.getDay() === 0) break;
    }
    return dias;
  });

  agenda = computed(() => {
    const desde = new Date(this.mes().getFullYear(), this.mes().getMonth(), 1).toISOString().slice(0, 10);
    const hasta = new Date(this.mes().getFullYear(), this.mes().getMonth() + 1, 0).toISOString().slice(0, 10);
    const map = new Map<string, CalendarioItem[]>();
    for (const it of this.items()) {
      const k = it.inicio.slice(0, 10);
      if (k < desde || k > hasta) continue;
      (map.get(k) ?? map.set(k, []).get(k)!).push(it);
    }
    return [...map.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([fecha, items]) => ({ fecha, items }));
  });

  cambiarMes(delta: number): void {
    const m = this.mes();
    this.mes.set(new Date(m.getFullYear(), m.getMonth() + delta, 1));
    this.cargarMes();
  }
  hoy(): void {
    const h = new Date();
    this.mes.set(new Date(h.getFullYear(), h.getMonth(), 1));
    this.diaSel.set(h.toISOString().slice(0, 10));
    this.cargarMes();
  }

  abrirItem(it: CalendarioItem): void {
    if (it.tipo === 'Reserva') {
      this.router.navigate(['/portal/reservas', it.id]);
      return;
    }
    this.eventoSel.set(it);
    this.vista.set('evento');
  }

  nuevoEvento(): void {
    this.form.set(this.formVacio());
    this.vista.set('nuevo');
  }
  setForm<K extends keyof CrearEvento>(k: K, v: CrearEvento[K]): void {
    this.form.update((f) => ({ ...f, [k]: v }));
  }
  formValido = computed(() => this.form().titulo.trim().length > 0 && this.form().inicio < this.form().fin);

  guardar(): void {
    if (!this.formValido() || this.guardando()) return;
    this.guardando.set(true);
    const f = this.form();
    const body: CrearEvento = {
      ...f,
      titulo: f.titulo.trim(),
      descripcion: f.descripcion?.trim() || null,
      ubicacion: f.ubicacion?.trim() || null,
      // enviamos la hora local tal cual (sin convertir a UTC)
      inicio: f.todoElDia ? f.inicio.slice(0, 10) + 'T00:00:00' : f.inicio,
      fin: f.todoElDia ? f.fin.slice(0, 10) + 'T23:59:00' : f.fin,
    };
    this.api.crearEvento(body).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito('Evento creado');
        this.vista.set('mes');
        this.cargarMes();
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo crear el evento.'); },
    });
  }

  compartirEvento(): void {
    const e = this.eventoSel();
    if (!e) return;
    const txt = `${e.titulo}\n${new Date(e.inicio).toLocaleString('es-AR')}${e.ubicacion ? '\n' + e.ubicacion : ''}`;
    if (navigator.share) navigator.share({ title: e.titulo, text: txt }).catch(() => {});
    else navigator.clipboard?.writeText(txt).then(() => this.toasts.exito('Copiado'));
  }
}
