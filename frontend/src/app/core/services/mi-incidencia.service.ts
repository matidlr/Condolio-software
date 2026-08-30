import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Incidencia {
  id: string;
  numero: number;
  titulo: string;
  descripcion: string;
  categoria: string;
  estado: string;
  prioridad: string;
  ubicacion?: string | null;
  creadoUtc: string;
  ultimaActividadUtc: string;
}

export interface IncidenciaMensaje {
  texto: string;
  autor: string;
  esAdministracion: boolean;
  fechaUtc: string;
}

export interface IncidenciaAdjunto {
  id: string;
  nombre: string;
  contentType: string;
  esImagen: boolean;
}

export interface IncidenciaDetalle {
  incidencia: Incidencia;
  mensajes: IncidenciaMensaje[];
  adjuntos: IncidenciaAdjunto[];
}

export const CATEGORIAS_INCIDENCIA = [
  { value: 'Mantenimiento', label: 'Mantenimiento', icon: '🔧' },
  { value: 'Seguridad', label: 'Seguridad', icon: '🛡' },
  { value: 'Amenidades', label: 'Amenidades', icon: '🏠' },
  { value: 'Mascotas', label: 'Mascotas', icon: '🐾' },
  { value: 'Ruido', label: 'Ruido', icon: '🔊' },
  { value: 'Vecinos', label: 'Vecinos', icon: '👥' },
  { value: 'Servicios', label: 'Servicios', icon: '💼' },
  { value: 'Otro', label: 'Otro', icon: '···' },
];

export const META_ESTADO_INC: Record<string, { color: string }> = {
  Pendiente: { color: '#d97706' },
  'En progreso': { color: '#2563eb' },
  'Necesita info': { color: '#7c3aed' },
  'En revisión': { color: '#0891b2' },
  Resuelto: { color: '#16a34a' },
};

@Injectable({ providedIn: 'root' })
export class MiIncidenciaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/incidencias`;

  listar(): Observable<Incidencia[]> {
    return this.http.get<Incidencia[]>(this.base);
  }
  obtener(id: string): Observable<IncidenciaDetalle> {
    return this.http.get<IncidenciaDetalle>(`${this.base}/${id}`);
  }
  crear(descripcion: string, categoria: string, archivos: File[]): Observable<Incidencia> {
    const form = new FormData();
    form.append('descripcion', descripcion);
    form.append('categoria', categoria);
    for (const f of archivos) form.append('archivos', f, f.name);
    return this.http.post<Incidencia>(this.base, form);
  }
  comentar(id: string, texto: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/comentarios`, { texto });
  }
  confirmar(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/confirmar`, {});
  }
  rechazar(id: string, motivo: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/rechazar`, { motivo });
  }
  adjuntoUrl(id: string): string {
    return `${this.base}/adjuntos/${id}`;
  }
}
