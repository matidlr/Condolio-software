import { Component, inject } from '@angular/core';
import { ToastService } from '../core/services/toast.service';

@Component({
  selector: 'app-toasts',
  standalone: true,
  template: `
    <div class="toasts">
      @for (t of toasts.toasts(); track t.id) {
        <div class="toast" [class]="'toast--' + t.tipo" (click)="toasts.cerrar(t.id)">
          <span class="toast__icon">{{ t.tipo === 'exito' ? '✓' : t.tipo === 'error' ? '!' : 'i' }}</span>
          <span>{{ t.mensaje }}</span>
        </div>
      }
    </div>
  `,
  styles: [`
    .toasts {
      position: fixed;
      top: 16px;
      right: 16px;
      z-index: 100;
      display: flex;
      flex-direction: column;
      gap: 8px;
      max-width: 360px;
    }
    .toast {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 16px;
      border-radius: 10px;
      background: #fff;
      border: 1px solid var(--c-border);
      box-shadow: 0 8px 30px rgba(15, 27, 45, 0.16);
      font-size: 0.9rem;
      cursor: pointer;
      animation: toast-in 0.18s ease;
    }
    .toast__icon {
      display: grid;
      place-items: center;
      width: 20px;
      height: 20px;
      border-radius: 50%;
      color: #fff;
      font-size: 0.72rem;
      font-weight: 700;
      flex-shrink: 0;
    }
    .toast--exito { border-left: 3px solid #16a34a; }
    .toast--exito .toast__icon { background: #16a34a; }
    .toast--error { border-left: 3px solid #dc2626; }
    .toast--error .toast__icon { background: #dc2626; }
    .toast--info { border-left: 3px solid #64748b; }
    .toast--info .toast__icon { background: #64748b; }
    @keyframes toast-in { from { opacity: 0; transform: translateX(12px); } to { opacity: 1; transform: none; } }
  `],
})
export class ToastsComponent {
  toasts = inject(ToastService);
}
