import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CrearInvitacion, Directorio, Invitacion, PersonaDetalle, ResidenteSinUnidad,
} from '../models/residente.models';

@Injectable({ providedIn: 'root' })
export class ResidenteService {
  private http = inject(HttpClient);

  /** Cantidad de invitaciones pendientes del consorcio activo (para el badge del menú). */
  readonly pendientes = signal(0);

  refrescarPendientes(consorcioId: string): void {
    this.invitaciones(consorcioId).subscribe({
      next: (list) => this.pendientes.set(list.filter((i) => i.estado === 'Pendiente').length),
      error: () => this.pendientes.set(0),
    });
  }

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/residentes`;
  }

  directorio(consorcioId: string): Observable<Directorio> {
    return this.http.get<Directorio>(this.base(consorcioId));
  }

  personaDetalle(consorcioId: string, personaId: string): Observable<PersonaDetalle> {
    return this.http.get<PersonaDetalle>(`${this.base(consorcioId)}/persona/${personaId}`);
  }

  actualizarContacto(consorcioId: string, personaId: string, body: { nombre: string; apellido: string; telefono?: string | null }): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/persona/${personaId}/contacto`, body);
  }

  removerDeComunidad(consorcioId: string, personaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/persona/${personaId}`);
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
