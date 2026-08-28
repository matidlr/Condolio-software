import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ResidenteService } from '../../core/services/residente.service';
import { UnidadService } from '../../core/services/unidad.service';
import { ToastService } from '../../core/services/toast.service';
import { Invitacion } from '../../core/models/residente.models';
import { Unidad } from '../../core/models/consorcio.models';
import { InvitarResidenteComponent } from './invitar-residente.component';
import { AyudaPanelComponent } from '../../shared/ayuda-panel.component';

@Component({
  selector: 'app-invitaciones',
  standalone: true,
  imports: [ReactiveFormsModule, InvitarResidenteComponent, AyudaPanelComponent],
  templateUrl: './invitaciones.component.html',
  styleUrl: './directorio.component.scss',
})
export class InvitacionesComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private toasts = inject(ToastService);
  consorcios = inject(ConsorcioService);
  private api = inject(ResidenteService);
  private unidadesApi = inject(UnidadService);

  todas = signal<Invitacion[]>([]);
  unidades = signal<Unidad[]>([]);
  busqueda = signal('');
  detalle = signal<Invitacion | null>(null);
  editar = signal<Invitacion | null>(null);
  reenviarModal = signal(false);
  invitarAbierto = signal(false);
  ayuda = signal(false);
  guardando = signal(false);
  error = signal<string | null>(null);
  seleccion = signal<Set<string>>(new Set());

  private cid = computed(() => this.consorcios.activoId());

  pendientes = computed(() => this.todas().filter((i) => i.estado === 'Pendiente'));
  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return this.pendientes().filter((i) => !q
      || (i.nombre ?? '').toLowerCase().includes(q) || i.email.toLowerCase().includes(q)
      || (i.unidadNombre ?? '').toLowerCase().includes(q));
  });

  formEditar = this.fb.nonNullable.group({
    nombre: [''],
    email: ['', [Validators.required, Validators.email]],
    unidadId: [''],
  });

  constructor() {
    effect(() => { const id = this.cid(); if (id) this.cargar(id); });
  }

  private cargar(id: string): void {
    this.api.invitaciones(id).subscribe((l) => this.todas.set(l));
    this.unidadesApi.listar(id).subscribe((u) => this.unidades.set(u));
  }

  refrescar(): void {
    const id = this.cid();
    if (id) this.cargar(id);
  }

  volver(): void { this.router.navigate(['/panel/unidades']); }

  fecha(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
  }
  fechaHora(iso: string): string {
    return new Date(iso).toLocaleString('es-AR', {
      day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
    }).replace(',', ' a las');
  }

  abrirEditar(inv: Invitacion): void {
    this.formEditar.reset({ nombre: inv.nombre ?? '', email: inv.email, unidadId: inv.unidadId ?? '' });
    this.error.set(null);
    this.detalle.set(null);
    this.editar.set(inv);
  }

  guardarEditar(): void {
    const id = this.cid();
    const inv = this.editar();
    if (!id || !inv || this.formEditar.invalid) { this.formEditar.markAllAsTouched(); return; }
    const v = this.formEditar.getRawValue();
    this.guardando.set(true);
    this.api.editarInvitacion(id, inv.id, {
      email: v.email.trim(), nombre: v.nombre.trim() || null,
      unidadId: v.unidadId || null, rol: inv.rol,
    }).subscribe({
      next: () => { this.guardando.set(false); this.toasts.exito('Invitación actualizada'); this.editar.set(null); this.refrescar(); },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo guardar.'); this.guardando.set(false); },
    });
  }

  reenviar(inv: Invitacion): void {
    const id = this.cid();
    if (!id) return;
    this.api.reenviar(id, inv.id).subscribe({
      next: () => { this.toasts.exito('Invitación reenviada'); this.detalle.set(null); this.refrescar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo reenviar.'),
    });
  }

  reenviarTodas(): void {
    const id = this.cid();
    if (!id) return;
    this.guardando.set(true);
    this.api.reenviarPendientes(id).subscribe({
      next: (n) => { this.guardando.set(false); this.reenviarModal.set(false); this.toasts.exito(`${n} invitación(es) reenviada(s)`); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo reenviar.'); },
    });
  }

  eliminar(inv: Invitacion): void {
    const id = this.cid();
    if (!id || !confirm(`¿Eliminar la invitación a ${inv.email}?`)) return;
    this.api.cancelar(id, inv.id).subscribe({
      next: () => { this.toasts.exito('Invitación eliminada'); this.detalle.set(null); this.refrescar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  onInvitado(): void {
    this.invitarAbierto.set(false);
    this.refrescar();
  }

  // ---- Selección ----
  todasSel = computed(() =>
    this.visibles().length > 0 && this.visibles().every((i) => this.seleccion().has(i.id)));

  toggleTodas(): void {
    const s = new Set(this.seleccion());
    if (this.todasSel()) this.visibles().forEach((i) => s.delete(i.id));
    else this.visibles().forEach((i) => s.add(i.id));
    this.seleccion.set(s);
  }

  toggleUna(id: string): void {
    const s = new Set(this.seleccion());
    s.has(id) ? s.delete(id) : s.add(id);
    this.seleccion.set(s);
  }

  reenviarSeleccionadas(): void {
    const id = this.cid();
    const ids = [...this.seleccion()];
    if (!id || !ids.length) return;
    this.guardando.set(true);
    forkJoin(ids.map((i) => this.api.reenviar(id, i))).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(`${ids.length} invitación(es) reenviada(s)`);
        this.seleccion.set(new Set());
        this.refrescar();
      },
      error: () => { this.guardando.set(false); this.toasts.error('No se pudieron reenviar todas.'); },
    });
  }

  eliminarSeleccionadas(): void {
    const id = this.cid();
    const ids = [...this.seleccion()];
    if (!id || !ids.length || !confirm(`¿Eliminar ${ids.length} invitación(es)?`)) return;
    this.guardando.set(true);
    forkJoin(ids.map((i) => this.api.cancelar(id, i))).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(`${ids.length} invitación(es) eliminada(s)`);
        this.seleccion.set(new Set());
        this.refrescar();
      },
      error: () => { this.guardando.set(false); this.toasts.error('No se pudieron eliminar todas.'); },
    });
  }

  iniciales(inv: Invitacion): string {
    const n = inv.nombre?.trim() || inv.email;
    return n.split(/[\s@.]+/).map((p) => p[0] ?? '').slice(0, 2).join('').toUpperCase();
  }
}
