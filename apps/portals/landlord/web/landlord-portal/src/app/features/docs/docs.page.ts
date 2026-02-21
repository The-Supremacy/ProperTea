import { Component, signal, inject, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { UserPreferencesService } from '../../core/services/user-preferences.service';
import { LogoComponent } from '../../../shared/components/logo';
import { HlmButton } from '@spartan-ng/helm/button';
import { ThemeToggleComponent } from '../../core/components/theme-toggle';
import { LanguageSelectorComponent } from '../../core/components/language-selector';

interface DocSection {
  id: string;
  title: string;
  items: DocItem[];
}

interface DocItem {
  id: string;
  title: string;
}

@Component({
  selector: 'app-docs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LogoComponent, HlmButton, ThemeToggleComponent, LanguageSelectorComponent, TranslocoPipe],
  template: `
    <div class="min-h-screen flex flex-col bg-background text-foreground">
      <!-- Header -->
      <header class="sticky top-0 z-40 border-b border-border bg-background/95 backdrop-blur">
        <div class="flex h-16 items-center px-4 sm:px-6 lg:px-8">
          <button
            hlmBtn
            variant="ghost"
            size="icon"
            class="lg:hidden mr-2"
            aria-label="Toggle navigation"
            [attr.aria-expanded]="mobileMenuOpen()"
            (click)="toggleMobileSidebar()"
          >
            <span aria-hidden="true">☰</span>
          </button>

          <app-logo size="md" />

          <div class="flex-1"></div>

          <div class="flex items-center gap-2">
            <app-language-selector />
            <app-theme-toggle />

            <button hlmBtn variant="outline" size="sm" (click)="navigateToHome()">
              {{ 'nav.home' | transloco }}
            </button>
          </div>
        </div>
      </header>

      <div class="flex-1 flex">
        <!-- Mobile Sidebar Overlay -->
        @if (mobileMenuOpen()) {
          <div
            class="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm lg:hidden"
            (click)="closeMobileSidebar()"
          ></div>
        }

        <!-- Sidebar -->
        <aside
          class="fixed lg:sticky top-16 z-40 h-[calc(100vh-4rem)] w-64 shrink-0 overflow-y-auto border-r border-border bg-background transition-transform lg:translate-x-0"
          [class.-translate-x-full]="!mobileMenuOpen()"
        >
          <nav class="p-4">
            @for (section of sections(); track section.id) {
              <div class="mb-6">
                <h3 class="mb-2 px-2 text-sm font-semibold text-foreground">
                  {{ section.title }}
                </h3>
                <ul class="space-y-1">
                  @for (item of section.items; track item.id) {
                    <li>
                      <button
                        hlmBtn
                        variant="ghost"
                        class="w-full justify-start"
                        [class.bg-accent]="selectedItem() === item.id"
                        (click)="selectItem(item.id)"
                      >
                        {{ item.title }}
                      </button>
                    </li>
                  }
                </ul>
              </div>
            }
          </nav>
        </aside>

        <!-- Content -->
        <main class="flex-1 overflow-y-auto">
          <div class="mx-auto max-w-4xl px-4 py-8 sm:px-6 lg:px-8">
            @switch (selectedItem()) {
              @case ('getting-started') {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>Getting Started</h1>
                  <p>
                    Welcome to ProperTea documentation. This guide will help you get started with
                    our property management platform.
                  </p>

                  <h2>Quick Start</h2>
                  <ol>
                    <li>Create your organization account</li>
                    <li>Set up your properties</li>
                    <li>Invite your team members</li>
                    <li>Start managing your operations</li>
                  </ol>

                  <h2>System Requirements</h2>
                  <p>ProperTea works best with modern web browsers including:</p>
                  <ul>
                    <li>Chrome (latest)</li>
                    <li>Firefox (latest)</li>
                    <li>Safari (latest)</li>
                    <li>Edge (latest)</li>
                  </ul>
                </article>
              }

              @case ('properties') {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>Managing Properties</h1>
                  <p>Learn how to add, edit, and organize your properties in ProperTea.</p>

                  <h2>Adding a Property</h2>
                  <p>To add a new property to your organization:</p>
                  <ol>
                    <li>Navigate to the Properties section</li>
                    <li>Click "Add Property"</li>
                    <li>Fill in the property details</li>
                    <li>Save your changes</li>
                  </ol>
                </article>
              }

              @case ('tenants') {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>Tenant Management</h1>
                  <p>Manage your tenants and their rental agreements efficiently.</p>

                  <h2>Adding Tenants</h2>
                  <p>Keep track of your tenants and their lease information in one place.</p>
                </article>
              }

              @case ('maintenance') {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>Maintenance Requests</h1>
                  <p>Track and manage maintenance requests from your tenants.</p>

                  <h2>Creating Work Orders</h2>
                  <p>Convert maintenance requests into actionable work orders.</p>
                </article>
              }

              @case ('api') {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>API Reference</h1>
                  <p>Access ProperTea programmatically using our REST API.</p>

                  <h2>Authentication</h2>
                  <p>All API requests require authentication using Bearer tokens.</p>
                </article>
              }

              @default {
                <article class="prose prose-slate max-w-none dark:prose-invert">
                  <h1>Documentation</h1>
                  <p>Select a topic from the sidebar to view detailed documentation.</p>
                </article>
              }
            }
          </div>
        </main>
      </div>
    </div>
  `,
})
export class DocsPage {
  private router = inject(Router);
  private preferencesService = inject(UserPreferencesService);

  protected mobileMenuOpen = signal(false);
  protected selectedItem = signal('getting-started');

  protected sections = signal<DocSection[]>([
    {
      id: 'general',
      title: 'General',
      items: [
        { id: 'getting-started', title: 'Getting Started' },
        { id: 'overview', title: 'Overview' },
      ],
    },
    {
      id: 'features',
      title: 'Features',
      items: [
        { id: 'properties', title: 'Properties' },
        { id: 'tenants', title: 'Tenants' },
        { id: 'maintenance', title: 'Maintenance' },
        { id: 'reporting', title: 'Reporting' },
      ],
    },
    {
      id: 'developers',
      title: 'Developers',
      items: [
        { id: 'api', title: 'API Reference' },
        { id: 'webhooks', title: 'Webhooks' },
      ],
    },
  ]);

  @HostListener('document:keydown.escape')
  protected onEscapeKey(): void {
    if (this.mobileMenuOpen()) {
      this.closeMobileSidebar();
    }
  }

  ngOnInit(): void {
    this.preferencesService.loadPreferences();
  }

  protected toggleMobileSidebar(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  protected closeMobileSidebar(): void {
    this.mobileMenuOpen.set(false);
  }

  protected selectItem(itemId: string): void {
    this.selectedItem.set(itemId);
    this.closeMobileSidebar();
  }

  protected navigateToHome(): void {
    this.router.navigate(['/']);
  }
}
