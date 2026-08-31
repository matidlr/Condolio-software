import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EstadoPase, PaseAcceso, TipoPase, TipoVehiculo, TipoVisita } from '../models/pase-acceso.models';

export interface PaseAdmin {
  id: string;
  codigo: string;
  tipoPase: TipoPase;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  visitanteNombre: string;
  patente?: string | null;
  fechaEntrada: string;
  validoHastaUtc?: string | null;
  estado: EstadoPase;
  creadoPor: string;
  creadoUtc: string;
  usosCount: number;
  usosMax: number;
  destino: string;
}

export interface PasesAdminLista {
  pases: PaseAdmin[];
  activos: number;
  generadosEnRango: number;
  escaneadosHoy: number;
}

export interface CrearPaseAdmin {
  unidadId?: string | null;
  tipoPase: TipoPase;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  visitanteNombre: string;
  patente?: string | null;
  fechaEntrada: string;
  validoHasta?: string | null;
}

export interface RegistroBitacora {
  id: string;
  visitanteNombre: string;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  patente?: string | null;
  unidad: string;
  ingresoUtc: string;
  egresoUtc?: string | null;
  registradoPor: string;
  nota?: string | null;
}

export interface Bitacora {
  registros: RegistroBitacora[];
  adentroAhora: number;
}

@Injectable({ providedIn: 'root' })
export class AccesoAdminService {
  private http = inject(HttpClient);
  private base(cid: string) { return `${environment.apiUrl}/consorcios/${cid}/seguridad`; }

  listarPases(cid: string, anio: number, mes: number, q = ''): Observable<PasesAdminLista> {
    return this.http.get<PasesAdminLista>(`${this.base(cid)}/pases`, { params: { anio, mes, q } });
  }
  obtenerPase(cid: string, id: string): Observable<PaseAcceso> {
    return this.http.get<PaseAcceso>(`${this.base(cid)}/pases/${id}`);
  }
  crearPase(cid: string, body: CrearPaseAdmin): Observable<PaseAcceso> {
    return this.http.post<PaseAcceso>(`${this.base(cid)}/pases`, body);
  }
  revocarPase(cid: string, id: string): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/pases/${id}/revocar`, {});
  }

  bitacora(cid: string, fecha: string, dias: number, filtro: string, q = ''): Observable<Bitacora> {
    return this.http.get<Bitacora>(`${this.base(cid)}/bitacora`, { params: { fecha, dias, filtro, q } });
  }
  egreso(cid: string, registroId: string): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/bitacora/${registroId}/egreso`, {});
  }
}
