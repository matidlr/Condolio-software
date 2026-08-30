import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-portal-proximamente',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="px">
      <div class="px-ic">🚧</div>
      <h2>Estamos preparando esta sección</h2>
      <p>Muy pronto vas a poder usar esta función desde el portal.</p>
      <a class="btn btn--primary" routerLink="/portal/casa">Volver al inicio</a>
    </div>
  `,
  styles: [`
    .px { text-align: center; padding: 60px 20px; }
    .px-ic { font-size: 2.6rem; }
    .px h2 { margin: 10px 0 6px; }
    .px p { margin: 0 auto 18px; max-width: 360px; color: var(--c-text-muted); }
    .px .btn { width: auto; padding: 0 22px; }
  `],
})
export class PortalProximamenteComponent {}
