export type EstadoEncuesta = 'Borrador' | 'Activa' | 'Cerrada';
export type CategoriaEncuesta = 'General' | 'Mantenimiento' | 'Evento';

export const CATEGORIAS_ENCUESTA: { value: CategoriaEncuesta; label: string; icon: string }[] = [
  { value: 'General', label: 'General', icon: '🌐' },
  { value: 'Mantenimiento', label: 'Mantenimiento', icon: '🔧' },
  { value: 'Evento', label: 'Eventos', icon: '📅' },
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
  multiplesOpciones: boolean;
  anonima: boolean;
  publicadaUtc?: string | null;
  cierreUtc?: string | null;
  autor: string;
  totalVotos: number;
  totalVotantes: number;
  yoVote: boolean;
  opciones: OpcionResultado[];
}

export interface Votante {
  nombre: string;
  opcion: string;
  fechaUtc: string;
}

export interface EncuestaDetalle {
  encuesta: Encuesta;
  votantes: Votante[];
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
  opciones: string[];
  multiplesOpciones: boolean;
  anonima: boolean;
  cierreUtc?: string | null;
  publicar: boolean;
}
