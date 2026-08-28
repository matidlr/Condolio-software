import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

type Tab = 'login' | 'registro';

const brandContent = {
  login: {
    titulo: 'Bienvenido a Condolio',
    tagline: 'Optimizá la gestión de tu propiedad con nuestra plataforma administrativa integral',
    features: [
      { icon: 'home', titulo: 'Gestión de Propiedades' },
      { icon: 'users', titulo: 'Portal de Residentes' },
      { icon: 'shield', titulo: 'Seguridad y Acceso' },
    ],
  },
  registro: {
    titulo: 'Unite a Condolio Hoy',
    tagline: 'Comenzá a gestionar tus propiedades con nuestra plataforma administrativa integral',
    features: [
      { icon: 'gear', titulo: 'Configuración Rápida y Fácil' },
      { icon: 'building', titulo: 'Herramientas de Gestión Integrales' },
      { icon: 'shield', titulo: 'Control de Acceso Seguro' },
    ],
  },
} as const;

function passwordsIguales(group: AbstractControl): ValidationErrors | null {
  const pass = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pass && confirm && pass !== confirm ? { passwordsDistintas: true } : null;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, NgTemplateOutlet],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  tab = signal<Tab>('login');
  cargando = signal(false);
  error = signal<string | null>(null);
  verPassword = signal(false);
  verConfirm = signal(false);

  brand = computed(() => brandContent[this.tab()]);

  loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  registroForm = this.fb.nonNullable.group(
    {
      nombre: ['', [Validators.required]],
      apellido: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
      acepto: [false, [Validators.requiredTrue]],
    },
    { validators: passwordsIguales },
  );

  cambiarTab(t: Tab): void {
    this.tab.set(t);
    this.error.set(null);
  }

  private pwValue = toSignal(this.registroForm.controls.password.valueChanges, { initialValue: '' });

  pwChecks = computed(() => {
    const p = this.pwValue() ?? '';
    return {
      largo: p.length >= 8,
      mayus: /[A-Z]/.test(p),
      minus: /[a-z]/.test(p),
      numero: /\d/.test(p),
    };
  });

  pwNivel = computed(() => {
    const c = this.pwChecks();
    return [c.largo, c.mayus, c.minus, c.numero].filter(Boolean).length;
  });

  pwLabel = computed(() => {
    const n = this.pwNivel();
    return n <= 1 ? { t: 'Débil', c: 1 } : n <= 3 ? { t: 'Media', c: 2 } : { t: 'Fuerte', c: 3 };
  });

  enviarLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.cargando.set(true);
    this.error.set(null);
    this.auth.login(this.loginForm.getRawValue()).subscribe({
      next: () => this.router.navigateByUrl(this.auth.rutaInicio()),
      error: (err) => {
        if (err?.status === 403 && err?.error?.requiereVerificacion) {
          this.router.navigate(['/verificar'], { queryParams: { email: err.error.email } });
          return;
        }
        this.error.set(err?.error?.message ?? 'No pudimos iniciar sesión.');
        this.cargando.set(false);
      },
    });
  }

  enviarRegistro(): void {
    if (this.registroForm.invalid) {
      this.registroForm.markAllAsTouched();
      return;
    }
    const { nombre, apellido, email, password } = this.registroForm.getRawValue();
    this.cargando.set(true);
    this.error.set(null);
    this.auth.registrar({ nombre, apellido, email, password }).subscribe({
      next: (res) => this.router.navigate(['/verificar'], { queryParams: { email: res.email } }),
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No pudimos crear la cuenta. Intentá de nuevo.');
        this.cargando.set(false);
      },
    });
  }
}
