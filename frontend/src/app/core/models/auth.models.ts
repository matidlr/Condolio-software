export type Rol = 'SuperAdmin' | 'Administrador' | 'Residente' | 'Personal';

export type AreaAdmin = 'Finanzas' | 'Operacion' | 'Seguridad' | 'Comunicacion' | 'Residentes';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegistroRequest {
  nombre: string;
  apellido: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiraUtc: string;
  email: string;
  nombre: string;
  roles: Rol[];
  adminGeneral?: boolean;
  adminAreas?: AreaAdmin[];
}

export interface RegistroResponse {
  requiereVerificacion: boolean;
  email: string;
}
