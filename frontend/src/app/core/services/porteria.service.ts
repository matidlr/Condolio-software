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

export interface UnidadRef { id: string; nombre: string; contacto?: string | null; }

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
}
export interface ResumenPaqueteria { porEntregar: number; llegaronHoy: number; entregadosHoy: number; }
export interface RegistrarPaquete {
  unidadId: string;
  tipo: TipoPaquete;
  cantidad: number;
  transportista?: string | null;
  descripcion?: string | null;
  llegadaLocal?: string | null;
  fotoBase64?: string | null;
}

export const TRANSPORTISTAS = [
  'Mercado Libre', 'Correo Argentino', 'OCA', 'Andreani', 'Cruz del Sur',
  'Vía Cargo', 'Urbano Express', 'Integra Retail (Fravega/Garbarino)', 'DHL', 'FedEx',
  'Entrega particular', 'Otro',
];

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

  paqueteResumen(): Observable<ResumenPaqueteria> { return this.http.get<ResumenPaqueteria>(`${this.base}/paquetes/resumen`); }
  paquetes(estado?: EstadoPaquete, q = '', anio = 0, mes = 0): Observable<{ paquetes: Paquete[] }> {
    const params: Record<string, string | number> = { q, anio, mes };
    if (estado) params['estado'] = estado;
    return this.http.get<{ paquetes: Paquete[] }>(`${this.base}/paquetes`, { params });
  }
  registrarPaquete(dto: RegistrarPaquete): Observable<Paquete> {
    return this.http.post<Paquete>(`${this.base}/paquetes`, dto);
  }
  entregarPaquete(id: string, retiradoPor: string | null): Observable<Paquete> {
    return this.http.post<Paquete>(`${this.base}/paquetes/${id}/entregar`, { retiradoPor });
  }

  personalCaseta(): Observable<PersonalTurno[]> { return this.http.get<PersonalTurno[]>(`${this.base}/personal`); }
  turnoActual(): Observable<TurnoActual | null> { return this.http.get<TurnoActual | null>(`${this.base}/turno`); }
  iniciarTurno(miembroPersonalId: string): Observable<TurnoActual> {
    return this.http.post<TurnoActual>(`${this.base}/turno`, { miembroPersonalId });
  }
  finalizarTurno(notas: string | null): Observable<void> {
    return this.http.post<void>(`${this.base}/turno/fin`, { notas });
  }
}
