import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificacionLista, NotificacionResumen, TipoNotificacion } from '../models/notificacion.models';

@Injectable({ providedIn: 'root' })
export class NotificacionService {
  private http = inject(HttpClient);

  readonly resumen = signal<NotificacionResumen>({ total: 0, noLeidas: 0 });

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/notificaciones`;
  }

  listar(consorcioId: string, opts: { soloNoLeidas?: boolean; tipo?: TipoNotificacion } = {}): Observable<NotificacionLista> {
    const params: Record<string, string> = {};
    if (opts.soloNoLeidas) params['soloNoLeidas'] = 'true';
    if (opts.tipo) params['tipo'] = opts.tipo;
    return this.http.get<NotificacionLista>(this.base(consorcioId), { params });
  }

  refrescarResumen(consorcioId: string): void {
    this.http.get<NotificacionResumen>(`${this.base(consorcioId)}/resumen`).subscribe({
      next: (r) => this.resumen.set(r),
      error: () => this.resumen.set({ total: 0, noLeidas: 0 }),
    });
  }

  marcarLeida(consorcioId: string, id: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/leida`, {});
  }

  marcarTodasLeidas(consorcioId: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/leidas`, {});
  }

  alternarLeida(consorcioId: string, id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.base(consorcioId)}/${id}/alternar-leida`, {});
  }

  alternarFijada(consorcioId: string, id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.base(consorcioId)}/${id}/fijar`, {});
  }
}
