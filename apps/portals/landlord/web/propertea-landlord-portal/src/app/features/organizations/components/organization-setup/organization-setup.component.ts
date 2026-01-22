import { Component, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  Subject,
  switchMap,
  debounceTime,
  distinctUntilChanged,
  catchError,
  of,
  takeUntil,
  tap,
  map,
} from 'rxjs';

// PrimeNG
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';

import { OrganizationService } from '../../services/organization.service';
import { AuthService } from '@features/auth/services/auth.service';

@Component({
  selector: 'app-org-setup',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, CardModule, MessageModule],
  templateUrl: './organization-setup.component.html', // Separate file
  styleUrls: ['./organization-setup.component.scss'],
})
export class OrganizationSetupComponent implements OnDestroy {
  private orgService = inject(OrganizationService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  // 1. Form State (Signals)
  name = signal('');
  slug = signal('');
  // Tracks if the user has manually edited the slug field
  slugManuallyEdited = signal(false);

  // 2. Validation State
  isCheckingSlug = signal(false);
  slugAvailable = signal<boolean | null>(null); // null = not checked, true = free, false = taken

  // 3. UI State
  isSubmitting = signal(false);
  serverErrors = signal<Record<string, string>>({});

  // 4. The Validation Pipeline (RxJS Subject)
  private availabilityCheck$ = new Subject<string>();

  constructor() {
    this.availabilityCheck$
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => {
          this.isCheckingSlug.set(true);
          this.slugAvailable.set(null);
          this.serverErrors.update((errs) => ({ ...errs, slug: '' }));
        }),
        switchMap((slug) => {
          if (!slug || slug.length < 3) return of(null); // Too short, don't call API

          // Avoid calling API when format is invalid (keeps UI consistent)
          const validFormat = /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug);
          if (!validFormat) return of(null);

          return this.orgService.checkSlug(slug).pipe(catchError(() => of(null)));
        }),

        takeUntil(this.destroy$),
      )
      .subscribe((isAvailable) => {
        this.isCheckingSlug.set(false);
        this.slugAvailable.set(isAvailable);
      });
  }

  onSlugChange(newValue: string) {
    this.slug.set(newValue);
    // Once the user edits the slug, stop auto-generating from name
    this.slugManuallyEdited.set(true);
    this.availabilityCheck$.next(newValue); // Push to the pipeline
  }

  onNameChange(newName: string) {
    this.name.set(newName);

    // Auto-generate slug from name until the user edits slug manually
    if (!this.slugManuallyEdited()) {
      const autoSlug = this.slugify(newName);
      this.slug.set(autoSlug);
      this.availabilityCheck$.next(autoSlug);
    }
  }

  async onSubmit() {
    this.isSubmitting.set(true);
    this.serverErrors.set({});

    try {
      const response = await this.orgService.create({
        name: this.name(),
        slug: this.slug(),
      });

      this.authService.login('/');
    } catch (err: any) {
      if (err.status === 422 && err.error?.errors) {
        const errors: Record<string, string> = {};
        for (const [key, msgs] of Object.entries(err.error.errors)) {
          if (Array.isArray(msgs) && msgs.length > 0) {
            errors[key.toLowerCase()] = msgs[0] as string;
          }
        }
        this.serverErrors.set(errors);
      }
    } finally {
      this.isSubmitting.set(false);
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private slugify(input: string): string {
    // Lowercase, replace non-alphanumerics with '-', collapse multiple '-', and trim '-' at ends
    const lowered = (input ?? '').toLowerCase();
    const replaced = lowered.replace(/[^a-z0-9]+/g, '-');
    const collapsed = replaced.replace(/-+/g, '-');
    const trimmed = collapsed.replace(/^-+|-+$/g, '');
    return trimmed;
  }
}
