import { Injectable } from '@angular/core';

/**
 * Configuration service for environment-specific URLs.
 * In K8s environments, all services use the same internal DNS names.
 */
@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  /**
   * ZITADEL URL for user profile management.
   * In K8s: Uses internal service name (e.g., http://zitadel:8080)
   * In local dev: Uses localhost with port
   */
  get zitadelUrl(): string {
    // Local development detection
    if (window.location.hostname === 'localhost') {
      return 'http://localhost:9080';
    }

    // K8s environments - all use the same internal service names
    // The ingress/gateway handles external routing
    return 'http://zitadel:8080';
  }

  /**
   * Get the current environment name for logging/debugging
   */
  get environment(): string {
    if (window.location.hostname === 'localhost') {
      return 'development';
    }

    // In K8s, you might want to inject this via environment variable
    // or determine it from hostname patterns
    const hostname = window.location.hostname;
    if (hostname.includes('dev.')) return 'development';
    if (hostname.includes('staging.')) return 'staging';
    if (hostname.includes('qa.')) return 'qa';
    return 'production';
  }
}
