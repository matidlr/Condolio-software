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
  rolesJunta: CargoJunta[];
}

export type CargoJunta = 'Presidente' | 'Vicepresidente' | 'Tesorero' | 'Secretario' | 'Miembro';

export const CARGOS_JUNTA: { value: CargoJunta; label: string; descripcion: string; icon: string }[] = [
  { value: 'Presidente', label: 'Presidente', descripcion: 'Supervisa las decisiones de la junta y representa a la comunidad', icon: '👑' },
  { value: 'Vicepresidente', label: 'Vicepresidente', descripcion: 'Asiste al presidente y actúa en su ausencia', icon: '🤝' },
  { value: 'Tesorero', label: 'Tesorero', descripcion: 'Administra las finanzas y los informes financieros', icon: '💲' },
  { value: 'Secretario', label: 'Secretario', descripcion: 'Registra reuniones y administra documentación', icon: '📄' },
  { value: 'Miembro', label: 'Miembro', descripcion: 'Participa en decisiones y actividades de la junta', icon: '👤' },
];

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
  rolesJunta: CargoJunta[];
  tieneCuenta: boolean;
  correoVerificado: boolean;
  activo: boolean;
  miembroDesdeUtc: string;
}
