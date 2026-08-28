import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-ayuda-panel',
  standalone: true,
  template: `
    <div class="ap-overlay" (click)="cerrar.emit()">
      <aside class="ap" (click)="$event.stopPropagation()">
        <header class="ap-head">
          <h2>{{ titulo() }}</h2>
          <button type="button" (click)="cerrar.emit()" aria-label="Cerrar">✕</button>
        </header>
        <div class="ap-body"><ng-content /></div>
      </aside>
    </div>
  `,
  styles: [`
    .ap-overlay {
      position: fixed; inset: 0;
      background: rgba(15, 27, 45, 0.35);
      display: flex; justify-content: flex-end;
      z-index: 60;
    }
    .ap {
      width: min(420px, 92vw);
      height: 100%;
      background: #fff;
      box-shadow: -8px 0 40px rgba(15, 27, 45, 0.18);
      display: flex; flex-direction: column;
      animation: ap-in 0.18s ease;
    }
    @keyframes ap-in { from { transform: translateX(20px); opacity: 0; } to { transform: none; opacity: 1; } }
    .ap-head {
      display: flex; align-items: center; justify-content: space-between;
      padding: 16px 20px;
      border-bottom: 1px solid var(--c-border);
    }
    .ap-head h2 { margin: 0; font-size: 1.05rem; }
    .ap-head button {
      width: 30px; height: 30px;
      border: none; background: transparent;
      border-radius: 8px; color: var(--c-text-muted);
    }
    .ap-head button:hover { background: #f3f6fb; }
    .ap-body { padding: 18px 20px; overflow-y: auto; font-size: 0.9rem; color: var(--c-text); line-height: 1.6; }
    .ap-body :is(h3, h4) { margin: 18px 0 6px; font-size: 0.95rem; }
    .ap-body :is(h3, h4):first-child { margin-top: 0; }
    .ap-body p { margin: 0 0 10px; color: var(--c-text-muted); }
    .ap-body ul, .ap-body ol { margin: 0 0 10px; padding-left: 20px; color: var(--c-text-muted); }
    .ap-body li { margin: 4px 0; }
    .ap-body a { color: var(--c-slate-600); }
  `],
})
export class AyudaPanelComponent {
  titulo = input('Ayuda');
  cerrar = output<void>();
}
