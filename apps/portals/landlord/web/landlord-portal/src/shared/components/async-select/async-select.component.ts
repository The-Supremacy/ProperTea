import {
  Component,
  input,
  output,
  signal,
  computed,
  effect,
  ChangeDetectionStrategy,
  untracked,
} from '@angular/core';
import {
  Combobox,
  ComboboxInput,
  ComboboxPopup,
  ComboboxPopupContainer,
} from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable, Subject, of, EMPTY } from 'rxjs';
import {
  switchMap,
  debounceTime,
  distinctUntilChanged,
  catchError,
  startWith,
  tap,
  finalize,
} from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { IconComponent } from '../icon';
import { SpinnerComponent } from '../spinner/spinner.component';

export interface AsyncSelectOption {
  value: string;
  label: string;
}

export type AsyncSelectFetchFn = (
  searchTerm: string,
  parentValues: Record<string, unknown>
) => Observable<AsyncSelectOption[]>;

@Component({
  selector: 'app-async-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Combobox,
    ComboboxInput,
    ComboboxPopup,
    ComboboxPopupContainer,
    Listbox,
    Option,
    OverlayModule,
    TranslocoPipe,
    IconComponent,
    SpinnerComponent,
  ],
  template: `
    <div ngCombobox class="relative">
      <div
        #origin
        class="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2"
        [class.opacity-50]="disabled()"
        (click)="onTriggerClick()">
        @if (!isOpen() || !searchable()) {
          <span class="flex-1 truncate">
            @if (displayValue()) {
              <span>{{ displayValue() }}</span>
            } @else {
              <span class="text-muted-foreground">{{ placeholder() | transloco }}</span>
            }
          </span>
        }
        <input
          #inputEl
          ngComboboxInput
          [attr.aria-label]="label() || placeholder()"
          [placeholder]="isOpen() && searchable() ? (searchPlaceholder() | transloco) : ''"
          [disabled]="disabled()"
          [class.sr-only]="!isOpen() || !searchable()"
          [class.flex-1]="isOpen() && searchable()"
          class="min-w-0 bg-transparent outline-none"
          (input)="onSearchInput($event)"
          (focus)="onFocus()" />
        @if (loading()) {
          <app-spinner size="sm" />
        } @else {
          <app-icon
            name="expand_more"
            [size]="20"
            class="transition-transform"
            [class.rotate-180]="isOpen()" />
        }
      </div>

      <ng-template ngComboboxPopupContainer>
        <ng-template
          cdkConnectedOverlay
          [cdkConnectedOverlayOrigin]="origin"
          [cdkConnectedOverlayOpen]="isOpen()"
          [cdkConnectedOverlayWidth]="origin.offsetWidth"
          (overlayOutsideClick)="close()">
          <div
            class="z-50 mt-1 max-h-60 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">
            @if (loading()) {
              <div class="flex items-center justify-center py-6 text-sm text-muted-foreground">
                <app-spinner size="sm" class="mr-2" />
                <span>{{ 'common.loading' | transloco }}</span>
              </div>
            } @else if (filteredOptions().length === 0) {
              <div class="py-6 text-center text-sm text-muted-foreground">
                {{ emptyMessage() | transloco }}
              </div>
            } @else {
              <div ngListbox [values]="selectedValues()" (valuesChange)="onSelectionChange($event)">
                @if (allowClear() && value()) {
                  <div
                    ngOption
                    value=""
                    label=""
                    class="relative flex cursor-pointer select-none items-center border-b px-3 py-2 text-sm text-muted-foreground outline-none hover:bg-accent hover:text-accent-foreground focus:bg-accent">
                    <app-icon name="close" [size]="16" class="mr-2" />
                    <span>{{ 'common.clear' | transloco }}</span>
                  </div>
                }
                @for (option of filteredOptions(); track option.value) {
                  <div
                    ngOption
                    [value]="option.value"
                    [label]="option.label"
                    class="relative flex cursor-pointer select-none items-center px-3 py-2 text-sm outline-none hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground data-selected:bg-accent data-selected:font-medium">
                    <span class="truncate">{{ option.label }}</span>
                    @if (option.value === value()) {
                      <app-icon name="check" [size]="16" class="ml-auto" />
                    }
                  </div>
                }
              </div>
            }
          </div>
        </ng-template>
      </ng-template>
    </div>
  `,
})
export class AsyncSelectComponent {
  // Inputs
  label = input<string>();
  placeholder = input('common.select');
  searchPlaceholder = input('common.typeToSearch');
  emptyMessage = input('common.noResults');
  value = input<string>('');
  disabled = input(false);
  searchable = input(true);
  allowClear = input(true);
  debounceMs = input(300);
  minSearchLength = input(0);

  /** Function to fetch options. Called with search term and parent filter values. */
  fetchFn = input.required<AsyncSelectFetchFn>();

  /** Current values of parent filters this select depends on. */
  parentValues = input<Record<string, unknown>>({});

  // Outputs
  valueChange = output<string>();
  opened = output<void>();
  closed = output<void>();

  // Internal state
  protected isOpen = signal(false);
  protected loading = signal(false);
  protected searchTerm = signal('');
  protected options = signal<AsyncSelectOption[]>([]);
  private optionsLoaded = signal(false);
  private searchSubject = new Subject<string>();

  // Computed
  protected selectedValues = computed(() => (this.value() ? [this.value()] : []));

  protected displayValue = computed(() => {
    const currentValue = this.value();
    if (!currentValue) return '';
    const option = this.options().find((opt) => opt.value === currentValue);
    return option?.label || currentValue;
  });

  protected filteredOptions = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const opts = this.options();

    if (!term || !this.searchable()) {
      return opts;
    }

    return opts.filter((opt) => opt.label.toLowerCase().includes(term));
  });

  constructor() {
    // Set up search debouncing with API fetch
    const searchResults$ = this.searchSubject.pipe(
      debounceTime(this.debounceMs()),
      distinctUntilChanged(),
      tap(() => this.loading.set(true)),
      switchMap((term) =>
        this.fetchFn()(term, this.parentValues()).pipe(
          catchError(() => of([])),
          finalize(() => this.loading.set(false))
        )
      )
    );

    // Convert to signal and update options
    effect(() => {
      const sub = searchResults$.subscribe((results) => {
        this.options.set(results);
        this.optionsLoaded.set(true);
      });
      return () => sub.unsubscribe();
    });

    // Reset options when parent values change
    effect(() => {
      const _ = this.parentValues(); // Track dependency
      untracked(() => {
        this.options.set([]);
        this.optionsLoaded.set(false);
        // Clear current value if it might no longer be valid
        if (this.value()) {
          this.valueChange.emit('');
        }
      });
    });
  }

  protected onTriggerClick(): void {
    if (this.disabled()) return;
    if (this.isOpen()) {
      this.close();
    } else {
      this.open();
    }
  }

  protected onFocus(): void {
    if (!this.isOpen() && !this.disabled()) {
      this.open();
    }
  }

  protected onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  protected onSelectionChange(values: string[]): void {
    const newValue = values[0] ?? '';
    if (newValue !== this.value()) {
      this.valueChange.emit(newValue);
    }
    this.close();
  }

  private open(): void {
    this.isOpen.set(true);
    this.opened.emit();

    // Load options on first open (lazy loading)
    if (!this.optionsLoaded()) {
      this.loadOptions();
    }
  }

  protected close(): void {
    this.isOpen.set(false);
    this.searchTerm.set('');
    this.closed.emit();
  }

  private loadOptions(): void {
    this.loading.set(true);
    this.fetchFn()('', this.parentValues())
      .pipe(
        catchError(() => of([])),
        finalize(() => this.loading.set(false))
      )
      .subscribe((results) => {
        this.options.set(results);
        this.optionsLoaded.set(true);
      });
  }
}
