import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizarTicket, CrearTicket, Ticket, TicketDetalle, TicketLista, UsuarioAsignable,
} from '../models/ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private http = inject(HttpClient);

  /** Tickets activos del consorcio activo (para el badge del menú). */
  readonly activos = signal(0);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/tickets`;
  }

  listar(consorcioId: string, archivados = false): Observable<TicketLista> {
    return this.http.get<TicketLista>(this.base(consorcioId), { params: { archivados } });
  }

  refrescarActivos(consorcioId: string): void {
    this.listar(consorcioId, false).subscribe({
      next: (l) => this.activos.set(l.activos),
      error: () => this.activos.set(0),
    });
  }

  asignables(consorcioId: string): Observable<UsuarioAsignable[]> {
    return this.http.get<UsuarioAsignable[]>(`${this.base(consorcioId)}/asignables`);
  }

  obtener(consorcioId: string, id: string): Observable<TicketDetalle> {
    return this.http.get<TicketDetalle>(`${this.base(consorcioId)}/${id}`);
  }

  crear(consorcioId: string, body: CrearTicket): Observable<Ticket> {
    return this.http.post<Ticket>(this.base(consorcioId), body);
  }

  actualizar(consorcioId: string, id: string, body: ActualizarTicket): Observable<Ticket> {
    return this.http.put<Ticket>(`${this.base(consorcioId)}/${id}`, body);
  }

  comentar(consorcioId: string, id: string, texto: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/comentarios`, { texto });
  }

  archivar(consorcioId: string, id: string, archivar: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/archivar`, { archivar });
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
