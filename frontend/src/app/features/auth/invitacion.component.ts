import { Component, inject, input, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

interface InvitacionPublica {
  consorcioNombre: string;
  email: string;
  nombre?: string | null;
  unidadNombre?: string | null;
  rol: string;
  valida: boolean;
  motivo?: string | null;
}

function passwordsIguales(g: AbstractControl): ValidationErrors | null {
  const p = g.get('password')?.value;
  const c = g.get('confirm')?.value;
  return p && c && p !== c ? { distintas: true } : null;
}

@Component({
  selector: 'app-invitacion',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './invitacion.component.html',
  styleUrl: './login/login.component.scss',
})
export class InvitacionComponent {
  token = input.required<string>();

  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  cargando = signal(true);
  invitacion = signal<InvitacionPublica | null>(null);
  error = signal<string | null>(null);
  enviando = signal(false);

  form = this.fb.nonNullable.group(
    {
      nombre: ['', [Validators.required]],
      apellido: ['', [Validators.required]],
      telefono: [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', [Validators.required]],
    },
    { validators: passwordsIguales },
  );

  ngOnInit(): void {
    this.http.get<InvitacionPublica>(`${environment.apiUrl}/invitaciones/${this.token()}`).subscribe({
      next: (inv) => {
        this.invitacion.set(inv);
        if (inv.nombre) {
          const [n, ...resto] = inv.nombre.split(/\s+/);
          this.form.patchValue({ nombre: n, apellido: resto.join(' ') });
        }
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No encontramos esta invitación.');
        this.cargando.set(false);
      },
    });
  }

  aceptar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.error.set(null);
    this.auth.aceptarInvitacion(this.token(), {
      nombre: v.nombre.trim(),
      apellido: v.apellido.trim(),
      telefono: v.telefono.trim() || null,
      password: v.password,
    }).subscribe({
      next: () => this.router.navigateByUrl(this.auth.rutaInicio()),
      error: (e) => {
        this.error.set(e?.error?.message ?? 'No pudimos activar tu cuenta.');
        this.enviando.set(false);
      },
    });
  }
}
