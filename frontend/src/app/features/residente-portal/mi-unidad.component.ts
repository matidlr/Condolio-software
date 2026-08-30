import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

interface MiUnidad {
  unidadId: string;
  unidadNombre: string;
  consorcioNombre: string;
  rol: string;
  esContactoPrincipal: boolean;
  cuotaMantenimiento?: number | null;
  saldo: number;
}

@Component({
  selector: 'app-mi-unidad',
  standalone: true,
  template: `
    <div class="mp">
      <main class="mp-body">
        <h1>Mi unidad</h1>

        @if (unidades().length) {
          <p class="mp-sub">{{ unidades()[0].consorcioNombre }}</p>
          <div class="mp-grid">
            @for (u of unidades(); track u.unidadId) {
              <article class="mp-card">
                <div class="mp-card__head">
                  <h2>Unidad {{ u.unidadNombre }}</h2>
                  <span class="mp-tag">{{ u.rol }}</span>
                </div>
                <dl>
                  <div><dt>Cuota mensual</dt><dd>{{ u.cuotaMantenimiento != null ? money(u.cuotaMantenimiento) : '—' }}</dd></div>
                  <div><dt>Saldo</dt><dd>{{ money(u.saldo) }}</dd></div>
                </dl>
                <div class="mp-soon">
                  <span>Expensas</span><span>Reservas</span><span>Comunicados</span><span>Reclamos</span>
                  <em>Próximamente</em>
                </div>
              </article>
            }
          </div>
        } @else if (cargando()) {
          <p>Cargando…</p>
        } @else {
          <div class="mp-card">
            <h2>Todavía no tenés una unidad asignada</h2>
            <p>Cuando el administrador te asigne una unidad, vas a verla acá.</p>
          </div>
        }
      </main>
    </div>
  `,
  // eslint-disable-next-line
  // (el header ahora lo provee portal-shell)
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--c-bg); }
    .mp-top {
      display: flex; align-items: center; justify-content: space-between;
      height: 60px; padding: 0 24px; background: #fff; border-bottom: 1px solid var(--c-border);
    }
    .mp-logo { display: flex; align-items: center; gap: 10px; font-weight: 700; }
    .mp-logo span { display: grid; place-items: center; width: 28px; height: 28px; border-radius: 8px;
      background: linear-gradient(150deg, #2b3a4e, #0f1b2d); color: #fff; }
    .mp-user { display: flex; align-items: center; gap: 12px; font-size: 0.9rem; }
    .mp-user button { border: 1px solid var(--c-border); background: #fff; border-radius: 8px; padding: 6px 12px; }
    .mp-body { max-width: 900px; margin: 0 auto; padding: 28px 24px; }
    .mp-body h1 { margin: 0 0 4px; }
    .mp-sub { margin: 0 0 22px; color: var(--c-text-muted); }
    .mp-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
    .mp-card { background: #fff; border: 1px solid var(--c-border); border-radius: var(--radius-lg); padding: 20px; }
    .mp-card__head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
    .mp-card__head h2 { margin: 0; font-size: 1.1rem; }
    .mp-tag { font-size: 0.75rem; font-weight: 600; background: #eef1f6; padding: 2px 10px; border-radius: 999px; }
    .mp-card dl { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin: 0 0 16px; }
    .mp-card dt { font-size: 0.75rem; color: var(--c-text-soft); text-transform: uppercase; letter-spacing: 0.05em; }
    .mp-card dd { margin: 4px 0 0; font-size: 1.3rem; letter-spacing: -0.02em; }
    .mp-soon { display: flex; flex-wrap: wrap; gap: 6px; padding-top: 14px; border-top: 1px solid var(--c-border); }
    .mp-soon span { font-size: 0.78rem; background: var(--c-bg); padding: 4px 10px; border-radius: 999px; color: var(--c-text-muted); }
    .mp-soon em { font-size: 0.72rem; color: var(--c-text-soft); align-self: center; }
  `],
})
export class MiUnidadComponent {
  auth = inject(AuthService);
  private http = inject(HttpClient);
  private router = inject(Router);

  unidades = signal<MiUnidad[]>([]);
  cargando = signal(true);

  ngOnInit(): void {
    this.http.get<{ nombre: string; unidades: MiUnidad[] }>(`${environment.apiUrl}/mi-unidad`).subscribe({
      next: (r) => { this.unidades.set(r.unidades); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }

  money(n: number | null | undefined): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2 });
  }

  salir(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
