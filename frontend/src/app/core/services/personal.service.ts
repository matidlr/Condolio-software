import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type TipoPersonal =
  | 'Seguridad' | 'Recepcionista' | 'Conserje' | 'Limpieza' | 'Mantenimiento'
  | 'Jardineria' | 'Piletero' | 'Administracion' | 'Subcontratista' | 'Otro';

export const TIPOS_PERSONAL: { value: TipoPersonal; label: string; icon: string }[] = [
  { value: 'Seguridad', label: 'Seguridad', icon: '🛡' },
  { value: 'Recepcionista', label: 'Recepcionista', icon: '🛎' },
  { value: 'Conserje', label: 'Conserje', icon: '🔑' },
  { value: 'Limpieza', label: 'Limpieza', icon: '🧹' },
  { value: 'Mantenimiento', label: 'Mantenimiento', icon: '🔧' },
  { value: 'Jardineria', label: 'Jardinería', icon: '🌿' },
  { value: 'Piletero', label: 'Piletero', icon: '💧' },
  { value: 'Administracion', label: 'Administración', icon: '🏢' },
  { value: 'Subcontratista', label: 'Subcontratista', icon: '📋' },
  { value: 'Otro', label: 'Otro', icon: '👤' },
];
export const LABEL_TIPO_PERSONAL: Record<TipoPersonal, string> =
  Object.fromEntries(TIPOS_PERSONAL.map((t) => [t.value, t.label])) as Record<TipoPersonal, string>;

export interface MiembroPersonal {
  id: string;
  nombre: string;
  apellido: string;
  tipo: TipoPersonal;
  tieneCuenta: boolean;
  email?: string | null;
  credencialId?: string | null;
  credencialNombre?: string | null;
  activo: boolean;
}
export interface PersonalLista {
  miembros: MiembroPersonal[];
  total: number;
  seguridad: number;
  conAcceso: number;
}
export interface GuardarPersonal {
  nombre: string;
  apellido: string;
  tipo: TipoPersonal;
  credencialId?: string | null;
}
export interface PersonalCreado {
  miembro: MiembroPersonal;
  passwordTemporal?: string | null;
}
export interface CredencialOpcion { id: string; nombre: string; email: string; }

export interface CredencialCaseta {
  id: string;
  nombre: string;
  email: string;
  activo: boolean;
  creadoUtc: string;
}
export interface CredencialesCasetaLista {
  dispositivos: CredencialCaseta[];
  total: number;
  activos: number;
  ultimoAgregadoUtc?: string | null;
  ultimoAgregadoNombre?: string | null;
}
export interface CredencialGenerada {
  dispositivo: CredencialCaseta;
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class PersonalService {
  private http = inject(HttpClient);
  private base(cid: string) { return `${environment.apiUrl}/consorcios/${cid}`; }

  // Staff
  listar(cid: string, q = ''): Observable<PersonalLista> {
    return this.http.get<PersonalLista>(`${this.base(cid)}/personal`, { params: { q } });
  }
  credencialesDisponibles(cid: string, incluirId?: string): Observable<CredencialOpcion[]> {
    return this.http.get<CredencialOpcion[]>(`${this.base(cid)}/personal/credenciales-disponibles`,
      { params: incluirId ? { incluirId } : {} });
  }
  crear(cid: string, body: GuardarPersonal): Observable<PersonalCreado> {
    return this.http.post<PersonalCreado>(`${this.base(cid)}/personal`, body);
  }
  actualizar(cid: string, id: string, body: GuardarPersonal): Observable<MiembroPersonal> {
    return this.http.put<MiembroPersonal>(`${this.base(cid)}/personal/${id}`, body);
  }
  eliminar(cid: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(cid)}/personal/${id}`);
  }

  // Credenciales de caseta
  credenciales(cid: string): Observable<CredencialesCasetaLista> {
    return this.http.get<CredencialesCasetaLista>(`${this.base(cid)}/credenciales-caseta`);
  }
  crearCredencial(cid: string, nombre: string): Observable<CredencialGenerada> {
    return this.http.post<CredencialGenerada>(`${this.base(cid)}/credenciales-caseta`, { nombre });
  }
  renombrarCredencial(cid: string, id: string, nombre: string): Observable<unknown> {
    return this.http.put(`${this.base(cid)}/credenciales-caseta/${id}/nombre`, { nombre });
  }
  restablecerCredencial(cid: string, id: string): Observable<CredencialGenerada> {
    return this.http.post<CredencialGenerada>(`${this.base(cid)}/credenciales-caseta/${id}/restablecer`, {});
  }
  eliminarCredencial(cid: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(cid)}/credenciales-caseta/${id}`);
  }
}
