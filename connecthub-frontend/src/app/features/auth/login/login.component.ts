import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  form: FormGroup;
  loading = false;
  error = '';
  showPass = false;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private signalR: SignalRService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({
      emailOrUsername: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    // Check for OAuth callback parameters
    this.route.queryParams.subscribe(params => {
      const token = params['token'];
      const user = params['user'];
      const userId = params['userId'];

      if (token && user && userId) {
        this.auth.handleOAuthCallback(token, user, +userId);
        this.signalR.startConnection();
        this.router.navigate(['/chat']);
      }
    });
  }

  get f() { return this.form.controls; }

  onSubmit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true; this.error = '';
    this.auth.login(this.form.value).subscribe({
      next: () => {
        this.signalR.startConnection();
        this.router.navigate(['/chat']);
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Invalid credentials. Please try again.';
      }
    });
  }

  get googleAuthUrl(): string {
    return `${environment.apiUrl}/auth/google-login`;
  }
}
