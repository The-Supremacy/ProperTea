import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect, viewChild, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { Combobox, ComboboxInput, ComboboxPopupContainer } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable, Subject, finalize, takeUntil } from 'rxjs';
import { IconComponent } from '../icon';
import { SpinnerComponent } from '../spinner';

export interface AutocompleteOption {
  label: string;
  value: string;
}

/**
 * Headless autocomplete component using Angular ARIA.
 * Provides filtering, keyboard navigation, and accessible selection.
 *
 * @example
 * ```typescript
 * <app-autocomplete
 *   [options]="companyOptions()"
 *   [value]="selectedCompanyId()"
 *   [placeholder]="'common.search' | transloco"
 *   (valueChange)="onCompanyChange($event)" />
 * ```
 */
@Component({
  selector: 'app-autocomplete',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Combobox,
    ComboboxInput,
    ComboboxPopupContainer,
    Listbox,
    Option,
    OverlayModule,
    TranslocoPipe,
    IconComponent,
    SpinnerComponent,
  ],
  templateUrl: './autocomplete.component.html',
  styleUrl: './autocomplete.component.css',
})
export class AutocompleteComponent implements OnInit, OnDestroy {
  // Inputs
  optionsProvider = input.required<() => Observable<AutocompleteOption[]>>();
  value = input<string>('');
  placeholder = input<string>('');
  disabled = input<boolean>(false);
  ariaLabel = input<string>('');
  expandOnFocus = input<boolean>(true);

  // Outputs
  valueChange = output<string>();

  // ViewChild
  protected inputElement = viewChild<ElementRef<HTMLInputElement>>('inputEl');

  // Internal state
  protected query = signal('');
  protected options = signal<AutocompleteOption[]>([]);
  protected loading = signal(false);
  private destroy$ = new Subject<void>();

  // Computed filtered options based on query
  protected filteredOptions = computed(() => {
    const q = this.query().toLowerCase();
    const opts = this.options();
    return q ? opts.filter((opt) => opt.label.toLowerCase().includes(q)) : opts;
  });

  // Sync query with selected value label
  constructor() {
    effect(() => {
      const selectedValue = this.value();
      const opts = this.options();

      // When value changes externally, update query to show label
      if (selectedValue) {
        const option = opts.find((opt) => opt.value === selectedValue);
        if (option) {
          this.query.set(option.label);
        }
      } else {
        this.query.set('');
      }
    });
  }

  ngOnInit(): void {
    this.loadOptions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadOptions(): void {
    this.loading.set(true);

    this.optionsProvider()()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (options) => {
          this.options.set(options);
        },
        error: (error) => {
          console.error('Failed to load autocomplete options:', error);
          this.options.set([]);
        },
      });
  }

  protected onQueryChange(newQuery: string): void {
    this.query.set(newQuery);
  }

  protected onInputClick(): void {
    if (this.expandOnFocus()) {
      const input = this.inputElement()?.nativeElement;
      if (input) {
        input.dispatchEvent(new Event('input', { bubbles: true }));
      }
    }
  }

  protected onSelect(value: string): void {
    this.valueChange.emit(value);
    // Query will be updated by effect when value input changes
  }
}
