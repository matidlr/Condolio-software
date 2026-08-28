import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  GuardarPersona,
  GuardarUnidad,
  PersonaUnidad,
  RolUnidad,
  TipoOcupacion,
  Unidad,
  UnidadDetalle,
} from '../models/consorcio.models';

@Injectable({ providedIn: 'root' })
export class UnidadService {
  private http = inject(HttpClient);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/unidades`;
  }

  listar(consorcioId: string): Observable<Unidad[]> {
    return this.http.get<Unidad[]>(this.base(consorcioId));
  }

  obtener(consorcioId: string, id: string): Observable<UnidadDetalle> {
    return this.http.get<UnidadDetalle>(`${this.base(consorcioId)}/${id}`);
  }

  crear(consorcioId: string, body: GuardarUnidad): Observable<Unidad> {
    return this.http.post<Unidad>(this.base(consorcioId), body);
  }

  crearLote(consorcioId: string, unidades: GuardarUnidad[], reemplazar = false): Observable<number> {
    return this.http.post<number>(`${this.base(consorcioId)}/lote`, { unidades, reemplazar });
  }

  importar(
    consorcioId: string,
    unidades: GuardarUnidad[],
    eliminarFaltantes = true,
  ): Observable<{ nuevas: number; actualizadas: number; eliminadas: number; totalDespues: number }> {
    return this.http.post<{ nuevas: number; actualizadas: number; eliminadas: number; totalDespues: number }>(
      `${this.base(consorcioId)}/importar`,
      { unidades, eliminarFaltantes },
    );
  }

  editarMasivo(consorcioId: string, items: unknown[]): Observable<number> {
    return this.http.put<number>(`${this.base(consorcioId)}/masivo`, { items });
  }

  actualizar(consorcioId: string, id: string, body: GuardarUnidad): Observable<Unidad> {
    return this.http.put<Unidad>(`${this.base(consorcioId)}/${id}`, body);
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }

  // ---- Ocupación y personas ----
  cambiarOcupacion(consorcioId: string, id: string, ocupacion: TipoOcupacion): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/${id}/ocupacion`, { ocupacion });
  }

  cambiarInquilinosFinanzas(consorcioId: string, id: string, permitir: boolean): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/${id}/inquilinos-finanzas`, { permitir });
  }

  agregarPersona(consorcioId: string, id: string, body: GuardarPersona): Observable<PersonaUnidad> {
    return this.http.post<PersonaUnidad>(`${this.base(consorcioId)}/${id}/personas`, body);
  }

  marcarPrincipal(consorcioId: string, id: string, personaId: string): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/${id}/personas/${personaId}/principal`, {});
  }

  cambiarRolPersona(consorcioId: string, id: string, personaId: string, rol: RolUnidad): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/${id}/personas/${personaId}/rol`, { rol });
  }

  eliminarPersona(consorcioId: string, id: string, personaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}/personas/${personaId}`);
  }
}
