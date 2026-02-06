import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { SessionService } from '../../core/services/session.service';
import { LogoComponent } from '../../../shared/components/logo';
import { ThemeToggleComponent } from '../../core/components/theme-toggle';
import { LanguageSelectorComponent } from '../../core/components/language-selector';
import { ButtonDirective } from '../../../shared/components/button/button.directive';

@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LogoComponent,
    ThemeToggleComponent,
    LanguageSelectorComponent,
    ButtonDirective,
    TranslocoPipe,
  ],
  template: `
    <div class="min-h-screen flex flex-col bg-background text-foreground">
      <!-- Header -->
      <header class="py-6 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto flex items-center justify-between">
          <app-logo size="lg" />

          <div class="flex items-center gap-2">
            <app-language-selector />
            <app-theme-toggle />
          </div>
        </div>
      </header>

      <!-- Hero Section -->
      <main class="flex-1 flex items-center justify-center px-4 sm:px-6 lg:px-8">
        <div class="max-w-4xl mx-auto text-center">
          <h1 class="text-5xl sm:text-6xl lg:text-7xl font-bold text-foreground mb-6">
            {{ 'landing.hero.title' | transloco }}
          </h1>

          <p class="text-xl sm:text-2xl text-muted-foreground mb-12 max-w-2xl mx-auto">
            {{ 'landing.hero.subtitle' | transloco }}
          </p>

          <!-- CTA Buttons -->
          <div class="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <button appBtn size="lg" (click)="navigateToRegister()">
              {{ 'landing.cta.getStarted' | transloco }}
            </button>

            <button appBtn variant="outline" size="lg" (click)="signIn()">
              {{ 'landing.cta.signIn' | transloco }}
            </button>

            <button appBtn variant="ghost" size="lg" (click)="navigateToDocs()">
              {{ 'landing.cta.documentation' | transloco }}
            </button>
          </div>
        </div>
      </main>

      <!-- Footer -->
      <footer class="py-6 px-4 sm:px-6 lg:px-8">
        <div class="max-w-7xl mx-auto text-center text-sm text-muted-foreground">
          <p>{{ 'footer.copyright' | transloco: { year: currentYear } }}</p>
        </div>
      </footer>
    </div>
  `,
})
export class LandingPage implements OnInit {
  private sessionService = inject(SessionService);
  private router = inject(Router);

  protected currentYear = new Date().getFullYear();

  ngOnInit(): void {
    if (this.sessionService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  protected navigateToRegister(): void {
    this.router.navigate(['/organizations/register']);
  }

  protected signIn(): void {
    this.sessionService.login();
  }

  protected navigateToDocs(): void {
    this.router.navigate(['/docs']);
  }
}
