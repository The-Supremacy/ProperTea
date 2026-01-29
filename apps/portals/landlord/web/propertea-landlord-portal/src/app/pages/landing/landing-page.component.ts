import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../auth/services/auth.service';
import { TranslocoModule } from '@jsverse/transloco';
import { LanguageSwitcherComponent } from '../../i18n/components/language-switcher.component';
import { FeatureCardComponent } from '../../shared/components';
import { inject } from '@angular/core';

@Component({
  selector: 'app-landing-page',
  imports: [ButtonModule, TranslocoModule, LanguageSwitcherComponent, FeatureCardComponent],
  templateUrl: './landing-page.component.html',
  styleUrl: './landing-page.component.scss'
})
export class LandingPageComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  currentYear = new Date().getFullYear();

  getStarted(): void {
    this.router.navigate(['/organizations/register']);
  }

  openDocumentation(): void {
    this.router.navigate(['/documentation']);
  }

  signIn(): void {
    this.authService.login();
  }
}
