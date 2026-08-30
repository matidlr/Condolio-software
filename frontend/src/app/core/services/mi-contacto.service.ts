import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Contacto {
  id: string;
  nombre: string;
  categoria: string;
  telefono: string;
  email?: string | null;
  empresa?: string | null;
  notas?: string | null;
  creadoPor: string;
  esMio: boolean;
  creadoUtc: string;
}

export interface GuardarContacto {
  nombre: string;
  categoria: string;
  telefono: string;
  email?: string | null;
  empresa?: string | null;
  notas?: string | null;
}

export const CATEGORIAS_CONTACTO = [
  'Plomería', 'Electricista', 'Gasista', 'Cerrajero', 'Jardinero', 'Pintor', 'Carpintero',
  'Limpieza', 'Servicio de Mascotas', 'Reparación de electrodomésticos',
  'Administración', 'Seguridad', 'Emergencias', 'Otro',
];

@Injectable({ providedIn: 'root' })
export class MiContactoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/contactos`;

  listar(): Observable<Contacto[]> {
    return this.http.get<Contacto[]>(this.base);
  }
  crear(body: GuardarContacto): Observable<Contacto> {
    return this.http.post<Contacto>(this.base, body);
  }
  actualizar(id: string, body: GuardarContacto): Observable<Contacto> {
    return this.http.put<Contacto>(`${this.base}/${id}`, body);
  }
  eliminar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
