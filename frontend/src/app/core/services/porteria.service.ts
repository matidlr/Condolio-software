import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TipoVehiculo, TipoVisita } from '../models/pase-acceso.models';

export interface PorteriaContexto { consorcioId: string; consorcioNombre: string; casetaNombre: string; }
export interface ResumenAcceso { adentroAhora: number; entradasHoy: number; salidasHoy: number; }

export interface RegistroBitacora {
  id: string;
  visitanteNombre: string;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  patente?: string | null;
  documento?: string | null;
  unidad: string;
  ingresoUtc: string;
  egresoUtc?: string | null;
  conQr: boolean;
  registradoPor: string;
  nota?: string | null;
}
export interface Bitacora { registros: RegistroBitacora[]; adentroAhora: number; }

export interface Verificacion {
  valido: boolean; motivo?: string | null; visitanteNombre: string;
  tipoVisita: TipoVisita; vehiculo: TipoVehiculo;
  patente?: string | null; unidadNombre: string; consorcioNombre: string; usosRestantes: number;
  token: string; fechaEntrada: string; validoHastaUtc?: string | null;
}

export interface EntradaManual {
  visitanteNombre: string;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  patente?: string | null;
  unidadId?: string | null;
  nota?: string | null;
  documento?: string | null;
}

export interface UnidadRef { id: string; nombre: string; }

export interface PersonalTurno { id: string; nombre: string; apellido: string; tipo: string; }
export interface TurnoActual { id: string; miembroPersonalId: string; personalNombre: string; inicioUtc: string; }
export interface ResidenteUnidad { nombre: string; rol: string; }
export interface UnidadDetalle { id: string; nombre: string; piso: number; residentes: ResidenteUnidad[]; }

@Injectable({ providedIn: 'root' })
export class PorteriaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/porteria`;

  contexto(): Observable<PorteriaContexto> { return this.http.get<PorteriaContexto>(`${this.base}/contexto`); }
  resumen(): Observable<ResumenAcceso> { return this.http.get<ResumenAcceso>(`${this.base}/resumen`); }
  verificar(token: string): Observable<Verificacion> { return this.http.post<Verificacion>(`${this.base}/verificar`, { token }); }
  confirmarIngreso(token: string, documento: string | null, patente: string | null): Observable<Verificacion> {
    return this.http.post<Verificacion>(`${this.base}/confirmar-ingreso`, { token, documento, patente });
  }
  entradaManual(dto: EntradaManual): Observable<RegistroBitacora> { return this.http.post<RegistroBitacora>(`${this.base}/entrada-manual`, dto); }
  adentro(): Observable<RegistroBitacora[]> { return this.http.get<RegistroBitacora[]>(`${this.base}/adentro`); }
  salida(id: string): Observable<void> { return this.http.post<void>(`${this.base}/salida/${id}`, {}); }
  bitacora(anio: number, mes: number, q = ''): Observable<Bitacora> {
    return this.http.get<Bitacora>(`${this.base}/bitacora`, { params: { anio, mes, q } });
  }
  unidades(): Observable<UnidadRef[]> { return this.http.get<UnidadRef[]>(`${this.base}/unidades`); }
  unidad(id: string): Observable<UnidadDetalle> { return this.http.get<UnidadDetalle>(`${this.base}/unidades/${id}`); }
  alertas(): Observable<{ anuncios: any[] }> { return this.http.get<{ anuncios: any[] }>(`${this.base}/alertas`); }

  personalCaseta(): Observable<PersonalTurno[]> { return this.http.get<PersonalTurno[]>(`${this.base}/personal`); }
  turnoActual(): Observable<TurnoActual | null> { return this.http.get<TurnoActual | null>(`${this.base}/turno`); }
  iniciarTurno(miembroPersonalId: string): Observable<TurnoActual> {
    return this.http.post<TurnoActual>(`${this.base}/turno`, { miembroPersonalId });
  }
  finalizarTurno(notas: string | null): Observable<void> {
    return this.http.post<void>(`${this.base}/turno/fin`, { notas });
  }
}
