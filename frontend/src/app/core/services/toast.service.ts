import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  tipo: 'exito' | 'error' | 'info';
  mensaje: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private seq = 0;

  exito(mensaje: string): void { this.push('exito', mensaje); }
  error(mensaje: string): void { this.push('error', mensaje); }
  info(mensaje: string): void { this.push('info', mensaje); }

  cerrar(id: number): void {
    this._toasts.update((l) => l.filter((t) => t.id !== id));
  }

  private push(tipo: Toast['tipo'], mensaje: string): void {
    const id = ++this.seq;
    this._toasts.update((l) => [...l, { id, tipo, mensaje }]);
    setTimeout(() => this.cerrar(id), tipo === 'error' ? 6000 : 3500);
  }
}
