import { RolUnidad } from './consorcio.models';

export interface Residente {
  id: string;
  nombre: string;
  apellido: string;
  email?: string | null;
  telefono?: string | null;
  rol: RolUnidad;
  esContactoPrincipal: boolean;
  tieneAcceso: boolean;
  unidadId: string;
  unidadNombre: string;
}

export interface Directorio {
  residentes: Residente[];
  total: number;
  propietarios: number;
  inquilinos: number;
  gestores: number;
  unidadesSinAsignar: number;
}

export type EstadoInvitacion = 'Pendiente' | 'Aceptada' | 'Expirada' | 'Cancelada';

export interface Invitacion {
  id: string;
  email: string;
  nombre?: string | null;
  estado: EstadoInvitacion;
  unidadId?: string | null;
  unidadNombre?: string | null;
  rol: RolUnidad;
  creadoUtc: string;
  expiraUtc: string;
}

export interface CrearInvitacion {
  email: string;
  nombre?: string | null;
  unidadId?: string | null;
  rol?: RolUnidad;
}

export interface ResidenteSinUnidad {
  usuarioId: string;
  nombre: string;
  apellido: string;
  email: string;
}

export interface PersonaUnidadRef {
  personaId: string;
  unidadId: string;
  unidadNombre: string;
  rol: RolUnidad;
  esContactoPrincipal: boolean;
}

export interface PersonaDetalle {
  nombre: string;
  apellido: string;
  email: string;
  telefono?: string | null;
  unidades: PersonaUnidadRef[];
  roles: string[];
  tieneCuenta: boolean;
  correoVerificado: boolean;
  activo: boolean;
  miembroDesdeUtc: string;
}
