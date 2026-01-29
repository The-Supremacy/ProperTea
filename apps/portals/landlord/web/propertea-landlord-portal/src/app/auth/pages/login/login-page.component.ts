import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule],
  template: `
    <div class="login-container flex align-items-center justify-content-center min-h-screen bg-gray-100">
      <p-card header="ProperTea Landlord Portal" class="w-full max-w-28rem shadow-4">
        <p class="text-center text-600 mb-5">Welcome back! Please sign in or register your organization to continue.</p>

        <div class="flex flex-column gap-3">
          <p-button label="Sign In" icon="pi pi-sign-in"
                    (onClick)="authService.login()" class="w-full">
          </p-button>

          <div class="relative flex align-items-center justify-content-center my-2">
            <div class="border-top-1 surface-border w-full absolute"></div>
            <span class="bg-white px-3 text-500 relative">OR</span>
          </div>

          <p-button label="Register" icon="pi pi-building"
                    [outlined]="true" severity="secondary"
                    (onClick)="router.navigate(['/organizations/register'])" class="w-full">
          </p-button>
        </div>
      </p-card>
    </div>
  `
})
export class LoginPageComponent {
  protected readonly authService = inject(AuthService);
  protected readonly router = inject(Router);
}
