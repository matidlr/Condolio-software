export type EstadoEncuesta = 'Borrador' | 'Activa' | 'Cerrada';
export type CategoriaEncuesta =
  | 'General' | 'Mantenimiento' | 'Evento' | 'Reglas' | 'Finanzas' | 'Emergencia';
export type ModoVotacion = 'PorResidente' | 'PorUnidad' | 'PonderadoPorAlicuota';
export type DuracionPreset = '1d' | '3d' | '1w' | '2w' | 'custom';

export const MODOS_VOTO: { k: ModoVotacion; label: string; icon: string; desc: string }[] = [
  { k: 'PorResidente', label: 'Un voto por residente', icon: '👥',
    desc: 'Cada residente elegible vota por sí mismo. Ideal para encuestas informales.' },
  { k: 'PorUnidad', label: 'Un voto por unidad', icon: '🏠',
    desc: 'Cada unidad emite un solo voto (propietario primero; el administrador de la unidad puede votar en su nombre).' },
  { k: 'PonderadoPorAlicuota', label: 'Ponderado por indiviso', icon: '％',
    desc: 'Un voto por unidad, con peso igual al indiviso de la unidad. Para decisiones formales.' },
];
export const LABEL_MODO_VOTO: Record<ModoVotacion, string> =
  Object.fromEntries(MODOS_VOTO.map((m) => [m.k, m.label])) as Record<ModoVotacion, string>;

export const DURACIONES: { k: DuracionPreset; label: string; dias: number | null }[] = [
  { k: '1d', label: '1 día', dias: 1 },
  { k: '3d', label: '3 días', dias: 3 },
  { k: '1w', label: '1 semana', dias: 7 },
  { k: '2w', label: '2 semanas', dias: 14 },
  { k: 'custom', label: 'Personalizado', dias: null },
];

export const CATEGORIAS_ENCUESTA: { value: CategoriaEncuesta; label: string; icon: string }[] = [
  { value: 'Mantenimiento', label: 'Mantenimiento', icon: '🔧' },
  { value: 'Evento', label: 'Eventos', icon: '📅' },
  { value: 'Reglas', label: 'Reglas', icon: '📋' },
  { value: 'Finanzas', label: 'Finanzas', icon: '💳' },
  { value: 'General', label: 'General', icon: '🌐' },
  { value: 'Emergencia', label: 'Emergencia', icon: '⚠️' },
];

export const LABEL_CAT_ENCUESTA: Record<CategoriaEncuesta, string> =
  Object.fromEntries(CATEGORIAS_ENCUESTA.map((c) => [c.value, c.label])) as Record<CategoriaEncuesta, string>;
export const ICON_CAT_ENCUESTA: Record<CategoriaEncuesta, string> =
  Object.fromEntries(CATEGORIAS_ENCUESTA.map((c) => [c.value, c.icon])) as Record<CategoriaEncuesta, string>;

export const META_ESTADO: Record<EstadoEncuesta, { label: string; color: string }> = {
  Borrador: { label: 'Borrador', color: '#64748b' },
  Activa: { label: 'En votación', color: '#16a34a' },
  Cerrada: { label: 'Cerrada', color: '#dc2626' },
};

export interface OpcionResultado {
  id: string;
  texto: string;
  votos: number;
  porcentaje: number;
  yoVote: boolean;
}

export interface Encuesta {
  id: string;
  titulo: string;
  descripcion: string;
  categoria: CategoriaEncuesta;
  estado: EstadoEncuesta;
  modoVotacion: ModoVotacion;
  multiplesOpciones: boolean;
  anonima: boolean;
  publicadaUtc?: string | null;
  cierreUtc?: string | null;
  autor: string;
  totalVotos: number;
  totalVotantes: number;
  yoVote: boolean;
  opciones: OpcionResultado[];
  creadoUtc: string;
}

export interface Votante {
  nombre: string;
  opcion: string;
  fechaUtc: string;
  unidad: string;
}

export interface VotoUnidad {
  unidad: string;
  votos: number;
  votantes: string[];
}

export interface EncuestaDetalle {
  encuesta: Encuesta;
  votantes: Votante[];
  unidadesTotales: number;
  votosPorUnidad: VotoUnidad[];
}

export interface EstadisticasEncuestas {
  total: number;
  activas: number;
  borradores: number;
  cerradas: number;
  totalVotos: number;
}

export interface EncuestaLista {
  encuestas: Encuesta[];
  estadisticas: EstadisticasEncuestas;
  general: number;
  mantenimiento: number;
  evento: number;
}

export interface GuardarEncuesta {
  titulo: string;
  descripcion: string;
  categoria: CategoriaEncuesta;
  modoVotacion: ModoVotacion;
  opciones: string[];
  multiplesOpciones: boolean;
  anonima: boolean;
  cierreUtc?: string | null;
  publicar: boolean;
}
