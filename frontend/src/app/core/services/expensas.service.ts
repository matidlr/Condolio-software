import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type TipoRubro = 'Ordinario' | 'Extraordinario' | 'FondoReserva';
export type CriterioDistribucion = 'PorCoeficiente' | 'PartesIguales';
export type FondoReservaTipo = 'Ninguno' | 'PorcentajeDeGastos' | 'MontoFijo';

export const CATEGORIAS_PROVEEDOR = [
  'Plomería', 'Electricidad', 'Aire Acondicionado', 'Limpieza', 'Cerrajería', 'Control de Plagas',
  'Jardinería', 'Pintura', 'Carpintería', 'Electrodomésticos', 'Seguridad', 'Mudanzas',
  'Ascensores', 'Administración', 'Seguros', 'Mantenimiento', 'Otro',
];

export interface ConfigExpensas {
  diaPrimerVencimiento: number;
  diaSegundoVencimiento: number | null;
  recargoSegundoVencimientoPct: number;
  tasaInteresMoraMensualPct: number;
  fondoReservaTipo: FondoReservaTipo;
  fondoReservaValor: number;
  inquilinoPagaOrdinarias: boolean;
  redondearAlPeso: boolean;
  mercadoPagoActivo: boolean;
  mercadoPagoTokenPreview: string | null;
}

export interface RubroGasto { id: string; nombre: string; tipo: TipoRubro; orden: number; esSistema: boolean; }

export interface Proveedor {
  id: string;
  nombre: string;
  empresa?: string | null;
  rubro?: string | null;
  cuit?: string | null;
  email?: string | null;
  telefono?: string | null;
  telefonoAlt?: string | null;
  direccion?: string | null;
  sitioWeb?: string | null;
  cbu?: string | null;
  alias?: string | null;
  horario?: string | null;
  notas?: string | null;
  activo: boolean;
  recomendado: boolean;
}
export interface ProveedoresLista {
  proveedores: Proveedor[];
  total: number; activos: number; inactivos: number; recomendados: number;
}
export type GuardarProveedor = Omit<Proveedor, 'id' | 'activo'>;

export interface Empleado {
  id: string;
  nombre: string; apellido: string;
  cuil?: string | null;
  categoria?: string | null;
  sueldoBasico: number;
  cargasSocialesPct: number;
  provisionaAguinaldo: boolean;
  otrosConceptosMensuales: number;
  rubroGastoId?: string | null;
  fechaIngreso?: string | null;
  activo: boolean;
  notas?: string | null;
  costoMensualTotal: number;
}
export type GuardarEmpleado = Omit<Empleado, 'id' | 'activo' | 'costoMensualTotal'>;

export interface GastoFijo {
  id: string;
  descripcion: string;
  rubroGastoId: string;
  rubroNombre: string;
  proveedorId?: string | null;
  proveedorNombre?: string | null;
  montoEstimado: number;
  criterioDistribucion: CriterioDistribucion;
  activo: boolean;
  notas?: string | null;
}
export type GuardarGastoFijo = Pick<GastoFijo, 'descripcion' | 'rubroGastoId' | 'proveedorId' | 'montoEstimado' | 'criterioDistribucion' | 'notas'>;

export interface GastosFijosResumen {
  empleados: Empleado[];
  gastos: GastoFijo[];
  totalEmpleados: number;
  totalGastos: number;
  totalMensual: number;
}

export type EstadoExtraordinaria = 'Activa' | 'Finalizada' | 'Cancelada';
export type CategoriaExtraordinaria =
  | 'MejorasCapital' | 'ReparacionEmergencia' | 'ProyectoEspecial' | 'Legales' | 'Equipamiento' | 'Otro';
export type MetodoReparto = 'Igual' | 'ProporcionalPorCoeficiente' | 'Personalizado';

export const CATEGORIAS_EXTRAORDINARIA: { v: CategoriaExtraordinaria; t: string }[] = [
  { v: 'MejorasCapital', t: 'Mejoras de capital y renovaciones' },
  { v: 'ReparacionEmergencia', t: 'Reparación de emergencia y mantenimiento' },
  { v: 'ProyectoEspecial', t: 'Proyecto especial' },
  { v: 'Legales', t: 'Legales' },
  { v: 'Equipamiento', t: 'Equipamiento' },
  { v: 'Otro', t: 'Otro' },
];

export interface ExtraordinariaUnidad {
  unidadId: string;
  unidadNombre: string;
  montoAsignado: number;
}
export interface Extraordinaria {
  id: string;
  titulo: string;
  descripcion?: string | null;
  categoria: CategoriaExtraordinaria;
  fechaInicio: string;
  fechaVencimiento?: string | null;
  metodoReparto: MetodoReparto;
  cantidadMeses: number;
  mesesEmitidos: number;
  montoTotal: number;
  montoPorMes: number;
  estado: EstadoExtraordinaria;
  notas?: string | null;
  unidades: ExtraordinariaUnidad[];
}
export interface CargoUnidadInput {
  unidadId: string;
  montoAsignado: number;
}
export interface GuardarExtraordinaria {
  titulo: string;
  descripcion?: string | null;
  categoria: CategoriaExtraordinaria;
  fechaInicio: string;
  fechaVencimiento?: string | null;
  metodoReparto: MetodoReparto;
  cantidadMeses: number;
  notas?: string | null;
  cargos: CargoUnidadInput[];
}
export interface ExtraordinariasLista {
  extraordinarias: Extraordinaria[];
  activas: number;
  totalEsperado: number;
  totalRecaudado: number;
}

export interface MorosidadUnidad {
  unidadId: string;
  nombre: string;
  piso: number;
  seccion?: string | null;
  saldoVencido: number;
  saldoPorVencer: number;
  diasAtraso: number;
  vencimientoMasAntiguo?: string | null;
  cargosVencidos: number;
}
export interface Morosidad {
  unidades: MorosidadUnidad[];
  alCorriente: number;
  morosas: number;
  porVencer: number;
  montoTotalMoroso: number;
  montoPorVencer: number;
}

@Injectable({ providedIn: 'root' })
export class ExpensasService {
  private http = inject(HttpClient);
  private base(cid: string): string { return `${environment.apiUrl}/consorcios/${cid}/expensas`; }

  // ---- config ----
  config(cid: string): Observable<ConfigExpensas> { return this.http.get<ConfigExpensas>(`${this.base(cid)}/config`); }
  guardarConfig(cid: string, dto: Partial<ConfigExpensas>): Observable<ConfigExpensas> {
    return this.http.put<ConfigExpensas>(`${this.base(cid)}/config`, dto);
  }
  guardarMercadoPago(cid: string, accessToken: string | null, publicKey: string | null): Observable<ConfigExpensas> {
    return this.http.put<ConfigExpensas>(`${this.base(cid)}/config/mercadopago`, { accessToken, publicKey });
  }

  // ---- rubros ----
  rubros(cid: string): Observable<RubroGasto[]> { return this.http.get<RubroGasto[]>(`${this.base(cid)}/rubros`); }
  crearRubro(cid: string, nombre: string, tipo: TipoRubro): Observable<RubroGasto> {
    return this.http.post<RubroGasto>(`${this.base(cid)}/rubros`, { nombre, tipo });
  }
  actualizarRubro(cid: string, id: string, nombre: string, tipo: TipoRubro): Observable<RubroGasto> {
    return this.http.put<RubroGasto>(`${this.base(cid)}/rubros/${id}`, { nombre, tipo });
  }
  eliminarRubro(cid: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(cid)}/rubros/${id}`); }

  // ---- proveedores ----
  proveedores(cid: string): Observable<ProveedoresLista> {
    return this.http.get<ProveedoresLista>(`${this.base(cid)}/proveedores`);
  }
  crearProveedor(cid: string, dto: GuardarProveedor): Observable<Proveedor> {
    return this.http.post<Proveedor>(`${this.base(cid)}/proveedores`, dto);
  }
  actualizarProveedor(cid: string, id: string, dto: GuardarProveedor): Observable<Proveedor> {
    return this.http.put<Proveedor>(`${this.base(cid)}/proveedores/${id}`, dto);
  }
  estadoProveedor(cid: string, id: string, activo: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/proveedores/${id}/estado`, { activo });
  }
  recomendarProveedor(cid: string, id: string, recomendado: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/proveedores/${id}/recomendado`, { recomendado });
  }

  // ---- gastos fijos + empleados ----
  gastosFijos(cid: string): Observable<GastosFijosResumen> {
    return this.http.get<GastosFijosResumen>(`${this.base(cid)}/gastos-fijos`);
  }
  crearEmpleado(cid: string, dto: GuardarEmpleado): Observable<Empleado> {
    return this.http.post<Empleado>(`${this.base(cid)}/empleados`, dto);
  }
  actualizarEmpleado(cid: string, id: string, dto: GuardarEmpleado): Observable<Empleado> {
    return this.http.put<Empleado>(`${this.base(cid)}/empleados/${id}`, dto);
  }
  estadoEmpleado(cid: string, id: string, activo: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/empleados/${id}/estado`, { activo });
  }
  crearGastoFijo(cid: string, dto: GuardarGastoFijo): Observable<GastoFijo> {
    return this.http.post<GastoFijo>(`${this.base(cid)}/gastos-fijos`, dto);
  }
  actualizarGastoFijo(cid: string, id: string, dto: GuardarGastoFijo): Observable<GastoFijo> {
    return this.http.put<GastoFijo>(`${this.base(cid)}/gastos-fijos/${id}`, dto);
  }
  estadoGastoFijo(cid: string, id: string, activo: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/gastos-fijos/${id}/estado`, { activo });
  }

  // ---- expensas extraordinarias ----
  extraordinarias(cid: string): Observable<ExtraordinariasLista> {
    return this.http.get<ExtraordinariasLista>(`${this.base(cid)}/extraordinarias`);
  }
  crearExtraordinaria(cid: string, dto: GuardarExtraordinaria): Observable<Extraordinaria> {
    return this.http.post<Extraordinaria>(`${this.base(cid)}/extraordinarias`, dto);
  }
  actualizarExtraordinaria(cid: string, id: string, dto: GuardarExtraordinaria): Observable<Extraordinaria> {
    return this.http.put<Extraordinaria>(`${this.base(cid)}/extraordinarias/${id}`, dto);
  }
  estadoExtraordinaria(cid: string, id: string, estado: EstadoExtraordinaria): Observable<void> {
    return this.http.post<void>(`${this.base(cid)}/extraordinarias/${id}/estado`, { estado });
  }

  // ---- morosidad ----
  morosidad(cid: string): Observable<Morosidad> {
    return this.http.get<Morosidad>(`${this.base(cid)}/morosidad`);
  }
}
