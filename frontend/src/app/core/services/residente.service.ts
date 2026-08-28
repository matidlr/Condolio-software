import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearInvitacion, Directorio, Invitacion, ResidenteSinUnidad } from '../models/residente.models';

@Injectable({ providedIn: 'root' })
export class ResidenteService {
  private http = inject(HttpClient);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/residentes`;
  }

  directorio(consorcioId: string): Observable<Directorio> {
    return this.http.get<Directorio>(this.base(consorcioId));
  }

  invitaciones(consorcioId: string): Observable<Invitacion[]> {
    return this.http.get<Invitacion[]>(`${this.base(consorcioId)}/invitaciones`);
  }

  invitar(consorcioId: string, body: CrearInvitacion): Observable<Invitacion> {
    return this.http.post<Invitacion>(`${this.base(consorcioId)}/invitaciones`, body);
  }

  editarInvitacion(consorcioId: string, id: string, body: CrearInvitacion): Observable<Invitacion> {
    return this.http.put<Invitacion>(`${this.base(consorcioId)}/invitaciones/${id}`, body);
  }

  invitarLote(
    consorcioId: string,
    items: { nombre: string; email: string; telefono: string; unidad: string; rol: string }[],
    notificar: boolean,
  ): Observable<{ enviadas: number; fallidas: number; filas: { email: string; ok: boolean; motivo?: string | null }[] }> {
    return this.http.post<{ enviadas: number; fallidas: number; filas: { email: string; ok: boolean; motivo?: string | null }[] }>(
      `${this.base(consorcioId)}/invitaciones/lote`,
      { items, notificar },
    );
  }

  reenviar(consorcioId: string, id: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/invitaciones/${id}/reenviar`, {});
  }

  reenviarPendientes(consorcioId: string): Observable<number> {
    return this.http.post<number>(`${this.base(consorcioId)}/invitaciones/reenviar-pendientes`, {});
  }

  porAsignar(consorcioId: string): Observable<ResidenteSinUnidad[]> {
    return this.http.get<ResidenteSinUnidad[]>(`${this.base(consorcioId)}/por-asignar`);
  }

  cancelar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/invitaciones/${id}`);
  }
}
