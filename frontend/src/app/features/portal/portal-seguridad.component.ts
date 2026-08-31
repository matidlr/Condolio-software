import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Location } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-portal-seguridad',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="psg">
      <header class="psg-top">
        <button type="button" class="psg-back" (click)="volver()">‹ Atrás</button>
        <h1>Cambiar contraseña</h1>
      </header>

      <div class="psg-card">
        <label class="psg-field">
          <span>Contraseña actual</span>
          <input [type]="ver() ? 'text' : 'password'" autocomplete="current-password"
            [ngModel]="actual()" (ngModelChange)="actual.set($event)" />
        </label>
        <label class="psg-field">
          <span>Nueva contraseña</span>
          <input [type]="ver() ? 'text' : 'password'" autocomplete="new-password"
            [ngModel]="nueva()" (ngModelChange)="nueva.set($event)" />
        </label>
        <div class="psg-meter"><span [style.width.%]="fuerza()"></span></div>
        @if (nueva().length > 0 && nueva().length < 6) {
          <small class="psg-hint">Faltan {{ 6 - nueva().length }} caracteres</small>
        }
        <label class="psg-field">
          <span>Confirmar nueva contraseña</span>
          <input [type]="ver() ? 'text' : 'password'" autocomplete="new-password"
            [ngModel]="confirmar()" (ngModelChange)="confirmar.set($event)" />
        </label>
        @if (confirmar().length > 0 && confirmar() !== nueva()) {
          <small class="psg-hint psg-hint--err">Las contraseñas no coinciden</small>
        }

        <label class="psg-ver">
          <input type="checkbox" [ngModel]="ver()" (ngModelChange)="ver.set($event)" /> Mostrar contraseñas
        </label>

        <div class="psg-actions">
          <button type="button" class="btn btn--ghost" (click)="volver()">Cancelar</button>
          <button type="button" class="btn btn--primary" [disabled]="!valido() || guardando()" (click)="guardar()">
            {{ guardando() ? 'Guardando…' : 'Guardar' }}
          </button>
        </div>
      </div>
    </section>
  `,
  styleUrl: './portal-seguridad.component.scss',
})
export class PortalSeguridadComponent {
  private auth = inject(AuthService);
  private toasts = inject(ToastService);
  private location = inject(Location);

  actual = signal('');
  nueva = signal('');
  confirmar = signal('');
  ver = signal(false);
  guardando = signal(false);

  fuerza = computed(() => Math.min(100, Math.round((this.nueva().length / 12) * 100)));
  valido = computed(() =>
    this.actual().length > 0 && this.nueva().length >= 6 && this.nueva() === this.confirmar());

  guardar(): void {
    if (!this.valido() || this.guardando()) return;
    this.guardando.set(true);
    this.auth.cambiarClave(this.actual(), this.nueva()).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito('Contraseña actualizada');
        this.volver();
      },
      error: (e) => {
        this.guardando.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo cambiar la contraseña.');
      },
    });
  }

  volver(): void {
    this.location.back();
  }
}
