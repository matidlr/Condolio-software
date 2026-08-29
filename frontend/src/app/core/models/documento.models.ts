export type NivelAcceso = 'Admin' | 'Propietarios' | 'Todos' | 'Junta';

export type CategoriaDocumento =
  | 'General' | 'ReglasYRegulaciones' | 'ActasReuniones' | 'Financiero'
  | 'LegalYContratos' | 'Recibos' | 'Mantenimiento';

export const CATEGORIAS_DOCUMENTO: { value: CategoriaDocumento; label: string }[] = [
  { value: 'Financiero', label: 'Financiero' },
  { value: 'LegalYContratos', label: 'Legal y contratos' },
  { value: 'ReglasYRegulaciones', label: 'Reglas y regulaciones' },
  { value: 'Recibos', label: 'Recibos' },
  { value: 'Mantenimiento', label: 'Mantenimiento' },
  { value: 'ActasReuniones', label: 'Actas de reuniones' },
  { value: 'General', label: 'General' },
];

export const LABEL_CATEGORIA_DOC: Record<CategoriaDocumento, string> =
  Object.fromEntries(CATEGORIAS_DOCUMENTO.map((c) => [c.value, c.label])) as Record<CategoriaDocumento, string>;

export const NIVELES_ACCESO: { value: NivelAcceso; label: string; color: string }[] = [
  { value: 'Admin', label: 'Solo admins', color: '#dc2626' },
  { value: 'Propietarios', label: 'Admins y propietarios', color: '#2563eb' },
  { value: 'Todos', label: 'Todos', color: '#14b8a6' },
];

/** Opciones jerárquicas para el diálogo "Compartir documento". */
export const NIVELES_COMPARTIR: {
  value: NivelAcceso; label: string; desc: string; icon: string; color: string;
}[] = [
  { value: 'Admin', label: 'Solo Admins', desc: 'Solo los administradores pueden acceder', icon: '🔒', color: '#dc2626' },
  { value: 'Junta', label: 'Miembros de la Junta', desc: 'Solo miembros de la junta', icon: '👑', color: '#7c3aed' },
  { value: 'Propietarios', label: 'Admins y Propietarios', desc: 'Administradores y propietarios', icon: '🏠', color: '#2563eb' },
  { value: 'Todos', label: 'Todos', desc: 'Todos los residentes incluyendo inquilinos', icon: '👥', color: '#14b8a6' },
];

export const META_NIVEL = Object.fromEntries(
  NIVELES_COMPARTIR.map((n) => [n.value, { value: n.value, label: n.label, color: n.color }]),
) as Record<NivelAcceso, { value: NivelAcceso; label: string; color: string }>;

export interface Carpeta {
  id: string;
  nombre: string;
  carpetaPadreId?: string | null;
  nivel: NivelAcceso;
  elementos: number;
}

export interface Documento {
  id: string;
  nombre: string;
  contentType: string;
  tamano: number;
  carpetaId?: string | null;
  nivel: NivelAcceso;
  categoria: CategoriaDocumento;
  destacado: boolean;
  creadoUtc: string;
  ultimoAccesoUtc?: string | null;
  subidoPor: string;
}

export interface Contenido {
  carpetaActualId?: string | null;
  carpetaActualNombre?: string | null;
  carpetas: Carpeta[];
  documentos: Documento[];
  almacenamientoUsado: number;
  almacenamientoTotal: number;
}

export interface CategoriaAgg {
  categoria: CategoriaDocumento;
  cantidad: number;
  tamano: number;
}

export interface DocPopular {
  id: string;
  nombre: string;
  categoria: CategoriaDocumento;
  vistas: number;
  descargas: number;
  ultimoAccesoUtc?: string | null;
}

export interface TimelinePunto {
  fecha: string;
  vistas: number;
  descargas: number;
}

export interface Analiticas {
  totalArchivos: number;
  almacenamientoUsado: number;
  almacenamientoTotal: number;
  totalVistas: number;
  totalDescargas: number;
  visoresUnicos: number;
  archivosCompartidos: number;
  actividadReciente: number;
  promedioDescargas: number;
  porCategoria: CategoriaAgg[];
  populares: DocPopular[];
  timeline: TimelinePunto[];
}
