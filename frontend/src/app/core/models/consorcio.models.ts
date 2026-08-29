export interface Consorcio {
  id: string;
  nombre: string;
  direccion: string;
  localidad?: string | null;
  provincia?: string | null;
  pais: string;
  cantidadUnidades: number;
}

export interface CrearConsorcio {
  nombre: string;
  direccion: string;
  localidad?: string | null;
  provincia?: string | null;
  pais?: string | null;
  cuit?: string | null;
}

export type TipoUnidad = 'Departamento' | 'Local' | 'Cochera' | 'Baulera';

export interface PersonaMini {
  nombre: string;
  esContactoPrincipal: boolean;
}

export interface Unidad {
  id: string;
  consorcioId: string;
  nombre: string;
  piso: number;
  tipo: TipoUnidad;
  ocupacion: TipoOcupacion;
  areaM2?: number | null;
  seccion?: string | null;
  cuotaMantenimiento?: number | null;
  coeficiente?: number | null;
  facturable: boolean;
  propietarios: PersonaMini[];
  inquilinos: PersonaMini[];
  gestores: PersonaMini[];
}

export type RolUnidad = 'Propietario' | 'Inquilino' | 'Gestor';

export type TipoOcupacion = 'HabitadoPorPropietario' | 'Alquiler' | 'Desocupado';

export const OCUPACIONES: { value: TipoOcupacion; label: string; hint: string }[] = [
  { value: 'HabitadoPorPropietario', label: 'Habitado por propietario', hint: 'El propietario vive en esta unidad' },
  { value: 'Alquiler', label: 'Alquiler', hint: 'Unidad alquilada a inquilinos' },
  { value: 'Desocupado', label: 'Desocupado', hint: 'Actualmente desocupada' },
];

export interface PersonaUnidad {
  id: string;
  nombre: string;
  apellido: string;
  email?: string | null;
  telefono?: string | null;
  rol: RolUnidad;
  esContactoPrincipal: boolean;
  tieneAcceso: boolean;
}

export interface NotaUnidad {
  id: string;
  texto: string;
  autor: string;
  autorUsuarioId: string;
  creadoUtc: string;
  actualizadoUtc?: string | null;
}

export type AdjuntoOwner = 'Nota' | 'Incidencia' | 'Ticket' | 'Amenidad';

export interface Adjunto {
  id: string;
  nombreArchivo: string;
  contentType: string;
  tamano: number;
  esImagen: boolean;
  creadoUtc: string;
}

export interface ActividadUnidad {
  id: string;
  tipo: string;
  titulo: string;
  detalle?: string | null;
  actor: string;
  creadoUtc: string;
}

export type CategoriaIncidencia =
  | 'Ruido' | 'Mascotas' | 'Seguridad' | 'Mantenimiento' | 'DanoPropiedad'
  | 'Visitante' | 'Disputa' | 'Cortesia' | 'Vecinos' | 'Otro';

export type SeveridadIncidencia = 'Baja' | 'Media' | 'Alta' | 'Critica';

export const CATEGORIAS_INCIDENCIA: { value: CategoriaIncidencia; label: string }[] = [
  { value: 'Ruido', label: 'Ruido' },
  { value: 'Mascotas', label: 'Mascotas' },
  { value: 'Seguridad', label: 'Seguridad' },
  { value: 'Mantenimiento', label: 'Mantenimiento' },
  { value: 'DanoPropiedad', label: 'Daño a propiedad' },
  { value: 'Visitante', label: 'Visitante' },
  { value: 'Disputa', label: 'Disputa' },
  { value: 'Cortesia', label: 'Cortesía' },
  { value: 'Vecinos', label: 'Vecinos' },
  { value: 'Otro', label: 'Otro' },
];

export const PLANTILLAS_INCIDENCIA: {
  label: string; categoria: CategoriaIncidencia; severidad: SeveridadIncidencia; titulo: string;
}[] = [
  { label: 'Queja por ruido', categoria: 'Ruido', severidad: 'Media', titulo: 'Queja por ruido' },
  { label: 'Falta relacionada con mascota', categoria: 'Mascotas', severidad: 'Media', titulo: 'Falta relacionada con mascota' },
  { label: 'Disputa de estacionamiento', categoria: 'Disputa', severidad: 'Media', titulo: 'Disputa de estacionamiento' },
  { label: 'Visitante no autorizado', categoria: 'Visitante', severidad: 'Alta', titulo: 'Visitante no autorizado' },
  { label: 'Daño en área común', categoria: 'DanoPropiedad', severidad: 'Alta', titulo: 'Daño en área común' },
  { label: 'Aviso de cortesía', categoria: 'Cortesia', severidad: 'Baja', titulo: 'Aviso de cortesía' },
];

export const LABEL_CATEGORIA: Record<CategoriaIncidencia, string> =
  Object.fromEntries(CATEGORIAS_INCIDENCIA.map((c) => [c.value, c.label])) as Record<CategoriaIncidencia, string>;

export interface IncidenciaUnidad {
  id: string;
  titulo?: string | null;
  descripcion: string;
  categoria: CategoriaIncidencia;
  severidad: SeveridadIncidencia;
  fechaEvento: string;
  etiquetas: string[];
  autor: string;
  creadoUtc: string;
  actualizadoUtc?: string | null;
  escaladaUtc?: string | null;
}

export interface IncidenciaHistorial {
  tipo: 'creada' | 'editada' | 'escalada' | 'comentario';
  texto: string;
  autor: string;
  fechaUtc: string;
}

export interface IncidenciaDetalle {
  incidencia: IncidenciaUnidad;
  historial: IncidenciaHistorial[];
}

export interface GuardarIncidencia {
  titulo?: string | null;
  descripcion: string;
  categoria: CategoriaIncidencia;
  severidad: SeveridadIncidencia;
  fechaEvento?: string | null;
  etiquetas?: string[] | null;
}

export interface GuardarPersona {
  nombre: string;
  apellido: string;
  email?: string | null;
  telefono?: string | null;
  rol: RolUnidad;
  esContactoPrincipal?: boolean;
  usuarioId?: string | null;
}

export interface UnidadDetalle {
  id: string;
  consorcioId: string;
  consorcioNombre: string;
  nombre: string;
  piso: number;
  tipo: TipoUnidad;
  ocupacion: TipoOcupacion;
  inquilinosVenFinanzas: boolean;
  areaM2?: number | null;
  seccion?: string | null;
  cuotaMantenimiento?: number | null;
  coeficiente?: number | null;
  facturable: boolean;
  ocupacionEfectiva: string;
  residentes: number;
  saldo: number;
  necesitaAtencion: boolean;
  personas: PersonaUnidad[];
}

export interface GuardarUnidad {
  nombre: string;
  piso: number;
  tipo: TipoUnidad;
  cuotaMantenimiento?: number | null;
  coeficiente?: number | null;
  facturable: boolean;
  areaM2?: number | null;
  seccion?: string | null;
}
