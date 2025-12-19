import { ApplicationConfig, provideBrowserGlobalErrorListeners, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideServiceWorker } from '@angular/service-worker';
import { routes } from './app.routes';

/**
 * Main application configuration.
 *
 * Note: No auth interceptor is needed because:
 * - Backend handles authentication via session cookies
 * - Cookies are automatically sent with every HTTP request (credentials: 'include')
 * - BFF pattern ensures all API calls go through authenticated proxy
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withFetch(), // Use Fetch API for HTTP calls
      withInterceptorsFromDi() // Enable DI-based interceptors if needed later
    ),
    provideAnimations(), // Deprecated but necessary for PrimeNG until v23
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000'
    })
  ]
};
