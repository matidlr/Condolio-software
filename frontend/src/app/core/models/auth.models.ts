export type Rol = 'SuperAdmin' | 'Administrador' | 'Residente' | 'Personal';

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
}

export interface RegistroResponse {
  requiereVerificacion: boolean;
  email: string;
}
