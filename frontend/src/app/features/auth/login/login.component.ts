import {
  Component, ElementRef, NgZone, computed, effect, inject, signal, viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Location, NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { environment } from '../../../../environments/environment';

declare const google: any;

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

/** Exige lo mismo que muestra la lista de requisitos: 6+ caracteres, mayúscula, minúscula y número. */
function passwordSegura(control: AbstractControl): ValidationErrors | null {
  const p: string = control.value ?? '';
  const ok = p.length >= 6 && /[A-Z]/.test(p) && /[a-z]/.test(p) && /\d/.test(p);
  return ok ? null : { passwordDebil: true };
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
  private location = inject(Location);
  private toasts = inject(ToastService);
  private zone = inject(NgZone);

  googleHost = viewChild<ElementRef<HTMLDivElement>>('googleHost');
  googleListo = signal(false);
  private gisInit = false;

  tab = signal<Tab>(
    inject(ActivatedRoute).snapshot.url.some((s) => s.path === 'registro') ? 'registro' : 'login',
  );
  cargando = signal(false);
  error = signal<string | null>(null);
  verPassword = signal(false);
  verConfirm = signal(false);
  registroOk = signal<string | null>(null);

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
      password: ['', [Validators.required, passwordSegura]],
      confirmPassword: ['', [Validators.required]],
      acepto: [false, [Validators.requiredTrue]],
    },
    { validators: passwordsIguales },
  );

  cambiarTab(t: Tab): void {
    this.tab.set(t);
    this.error.set(null);
    this.location.replaceState(t === 'registro' ? '/registro' : '/login');
  }

  constructor() {
    // Renderiza el botón de Google cuando el contenedor está en el DOM (cambia con el tab).
    effect(() => {
      const host = this.googleHost();
      this.tab();
      if (host) this.montarGoogle(host.nativeElement);
    });
  }

  private montarGoogle(el: HTMLElement, intentos = 20): void {
    const clientId = environment.googleClientId;
    if (!clientId) return;
    if (typeof google === 'undefined' || !google?.accounts?.id) {
      if (intentos > 0) setTimeout(() => this.montarGoogle(el, intentos - 1), 250);
      return;
    }
    if (!this.gisInit) {
      google.accounts.id.initialize({
        client_id: clientId,
        callback: (resp: { credential?: string }) =>
          this.zone.run(() => this.entrarConGoogle(resp.credential)),
      });
      this.gisInit = true;
    }
    el.innerHTML = '';
    google.accounts.id.renderButton(el, {
      type: 'standard', theme: 'outline', size: 'large', shape: 'rectangular',
      text: this.tab() === 'registro' ? 'signup_with' : 'signin_with',
      logo_alignment: 'center', width: 340,
    });
    this.googleListo.set(true);
  }

  googleNoConfigurado(): void {
    this.toasts.info('Configurá el Client ID de Google para habilitar este acceso.');
  }

  private entrarConGoogle(credential?: string): void {
    if (!credential) return;
    this.cargando.set(true);
    this.error.set(null);
    this.auth.googleLogin(credential).subscribe({
      next: () => this.router.navigateByUrl(this.auth.rutaInicio()),
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No pudimos entrar con Google.');
        this.cargando.set(false);
      },
    });
  }

  private pwValue = toSignal(this.registroForm.controls.password.valueChanges, { initialValue: '' });

  pwChecks = computed(() => {
    const p = this.pwValue() ?? '';
    return {
      largo: p.length >= 6,
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
      next: (res) => {
        this.cargando.set(false);
        this.registroOk.set(res.email);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'No pudimos crear la cuenta. Intentá de nuevo.');
        this.cargando.set(false);
      },
    });
  }

  irAVerificar(): void {
    const email = this.registroOk();
    this.registroOk.set(null);
    this.router.navigate(['/verificar'], { queryParams: { email } });
  }
}
