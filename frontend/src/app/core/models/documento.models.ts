export type NivelAcceso = 'Admin' | 'Propietarios' | 'Todos';

export const NIVELES_ACCESO: { value: NivelAcceso; label: string; color: string }[] = [
  { value: 'Admin', label: 'Admin', color: '#dc2626' },
  { value: 'Propietarios', label: 'Propietarios', color: '#2563eb' },
  { value: 'Todos', label: 'Todos', color: '#14b8a6' },
];

export const META_NIVEL = Object.fromEntries(
  NIVELES_ACCESO.map((n) => [n.value, n]),
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
