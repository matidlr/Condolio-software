import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearReserva, EstadoReserva, Reserva, ReservaLista } from '../models/reserva.models';

@Injectable({ providedIn: 'root' })
export class ReservaService {
  private http = inject(HttpClient);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/reservas`;
  }

  listar(consorcioId: string, desde: string, hasta: string): Observable<ReservaLista> {
    return this.http.get<ReservaLista>(this.base(consorcioId), { params: { desde, hasta } });
  }

  crear(consorcioId: string, body: CrearReserva): Observable<Reserva> {
    return this.http.post<Reserva>(this.base(consorcioId), body);
  }

  cambiarEstado(consorcioId: string, id: string, estado: EstadoReserva): Observable<Reserva> {
    return this.http.post<Reserva>(`${this.base(consorcioId)}/${id}/estado`, { estado });
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
