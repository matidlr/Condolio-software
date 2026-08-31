import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type TipoPaquete = 'Paquete' | 'Correo' | 'Otro';
export type EstadoPaquete = 'EnRecepcion' | 'Entregado';

export interface Paquete {
  id: string;
  unidadId: string;
  unidadNombre: string;
  tipo: TipoPaquete;
  cantidad: number;
  transportista?: string | null;
  descripcion?: string | null;
  estado: EstadoPaquete;
  llegadaUtc: string;
  entregaUtc?: string | null;
  registradoPorNombre: string;
  entregadoPorNombre?: string | null;
  retiradoPorNombre?: string | null;
  tieneFoto: boolean;
}

export interface ResumenPaqueteria {
  porEntregar: number;
  llegaronHoy: number;
  entregadosHoy: number;
  total: number;
  necesitanAtencion: number;
}

export interface PaqueteDetalle {
  paquete: Paquete;
  referencia: string;
  residentes: string[];
  fotoDataUrl?: string | null;
}

export const LABEL_TIPO_PAQUETE: Record<TipoPaquete, string> = {
  Paquete: 'Paquete',
  Correo: 'Correo / sobre',
  Otro: 'Otro',
};

@Injectable({ providedIn: 'root' })
export class PaqueteAdminService {
  private http = inject(HttpClient);

  private base(cid: string): string {
    return `${environment.apiUrl}/consorcios/${cid}/paquetes`;
  }

  resumen(cid: string): Observable<ResumenPaqueteria> {
    return this.http.get<ResumenPaqueteria>(`${this.base(cid)}/resumen`);
  }
  listar(cid: string, estado?: EstadoPaquete, q = ''): Observable<{ paquetes: Paquete[] }> {
    const params: Record<string, string> = { q };
    if (estado) params['estado'] = estado;
    return this.http.get<{ paquetes: Paquete[] }>(this.base(cid), { params });
  }
  detalle(cid: string, id: string): Observable<PaqueteDetalle> {
    return this.http.get<PaqueteDetalle>(`${this.base(cid)}/${id}`);
  }
  entregar(cid: string, id: string, retiradoPor: string | null = null): Observable<Paquete> {
    return this.http.post<Paquete>(`${this.base(cid)}/${id}/entregar`, { retiradoPor });
  }
  recordatorio(cid: string, id: string): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/${id}/recordatorio`, {});
  }
}
