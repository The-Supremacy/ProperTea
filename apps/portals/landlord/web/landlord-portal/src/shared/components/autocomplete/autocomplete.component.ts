import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { HlmAutocompleteImports } from '@spartan-ng/helm/autocomplete';
import { HlmSpinner } from '@spartan-ng/helm/spinner';

export interface AutocompleteOption {
  label: string;
  value: string;
}

/**
 * Autocomplete dropdown with async option loading.
 * Backed by Spartan brain — handles keyboard nav, a11y, and dropdown management.
 *
 * @example
 * ```html
 * <app-autocomplete
 *   [optionsProvider]="getOptions"
 *   [value]="selectedId()"
 *   [placeholder]="'common.search' | transloco"
 *   (valueChange)="selectedId.set($event)" />
 * ```
 */
@Component({
  selector: 'app-autocomplete',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmAutocompleteImports, HlmSpinner, TranslocoPipe],
  templateUrl: './autocomplete.component.html',
})
export class AutocompleteComponent {
  readonly optionsProvider = input.required<() => Observable<AutocompleteOption[]>>();
  readonly value = input<string>('');
  readonly placeholder = input<string>('');
  readonly disabled = input<boolean>(false);
  readonly ariaLabel = input<string>('');

  readonly valueChange = output<string>();

  private readonly translocoService = inject(TranslocoService);

  protected readonly translatedPlaceholder = computed(() => {
    const key = this.placeholder();
    return key ? this.translocoService.translate(key) : '';
  });

  // rxResource: loads options, auto-cancels on destroy, reacts to optionsProvider changes.
  protected readonly data = rxResource< AutocompleteOption[], () => Observable<AutocompleteOption[]>>({
    params: () => this.optionsProvider(),
    stream: ({ params: provider }) => provider(),
  });

  // Brain drives search input; we filter locally (options are loaded once).
  protected readonly search = signal('');

  protected readonly filteredOptions = computed(() => {
    const q = this.search().toLowerCase();
    const opts = this.data.value() ?? [];
    return q ? opts.filter((o) => o.label.toLowerCase().includes(q)) : opts;
  });

  protected readonly selectedOption = computed<AutocompleteOption | null>(() => {
    const id = this.value();
    return this.data.value()?.find((o) => o.value === id) ?? null;
  });

  protected readonly isItemEqualToValue = (
    item: AutocompleteOption,
    selected: AutocompleteOption | null,
  ): boolean => item.value === selected?.value;

  protected onValueChange(option: AutocompleteOption | null): void {
    this.valueChange.emit(option?.value ?? '');
  }
}
