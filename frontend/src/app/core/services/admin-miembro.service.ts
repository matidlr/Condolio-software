import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type AreaAdmin = 'Finanzas' | 'Operacion' | 'Seguridad' | 'Comunicacion' | 'Residentes';

export const AREAS_ADMIN: { value: AreaAdmin; label: string; icon: string }[] = [
  { value: 'Finanzas', label: 'Finanzas', icon: '💳' },
  { value: 'Operacion', label: 'Operación', icon: '🔧' },
  { value: 'Seguridad', label: 'Seguridad', icon: '🛡️' },
  { value: 'Comunicacion', label: 'Comunicación', icon: '📣' },
  { value: 'Residentes', label: 'Residentes', icon: '👥' },
];

export const LABEL_AREA: Record<AreaAdmin, string> =
  Object.fromEntries(AREAS_ADMIN.map((a) => [a.value, a.label])) as Record<AreaAdmin, string>;

export interface AdminMiembro {
  usuarioId: string;
  nombre: string;
  email: string;
  esGeneral: boolean;
  esDueno: boolean;
  areas: AreaAdmin[];
}

@Injectable({ providedIn: 'root' })
export class AdminMiembroService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/administradores`;

  listar(): Observable<AdminMiembro[]> {
    return this.http.get<AdminMiembro[]>(this.base);
  }
  agregar(email: string, esGeneral: boolean, areas: AreaAdmin[]): Observable<AdminMiembro> {
    return this.http.post<AdminMiembro>(this.base, { email, esGeneral, areas });
  }
  cambiarRol(usuarioId: string, esGeneral: boolean, areas: AreaAdmin[]): Observable<AdminMiembro> {
    return this.http.put<AdminMiembro>(`${this.base}/${usuarioId}/rol`, { esGeneral, areas });
  }
  quitar(usuarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${usuarioId}`);
  }
}
