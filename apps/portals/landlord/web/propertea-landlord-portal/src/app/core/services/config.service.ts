import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ConfigService {
  get idpUrl(): string {
    if (window.location.hostname === 'localhost') {
      return 'http://localhost:9080';
    }

    return 'http://keycloak:8080';
  }

  get realm(): string {
    return 'ProperTea';
  }

  get environment(): string {
    if (window.location.hostname === 'localhost') {
      return 'development';
    }

    const hostname = window.location.hostname;
    if (hostname.includes('dev.')) return 'development';
    if (hostname.includes('staging.')) return 'staging';
    if (hostname.includes('qa.')) return 'qa';
    return 'production';
  }
}
