import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable } from 'rxjs';

export interface SelectOptionData {
  value: string;
  label: string;
}

/**
 * Select dropdown with async option loading.
 * Backed by Spartan BrnSelect — handles keyboard nav, a11y, and overlay management.
 *
 * @example
 * ```html
 * <app-select
 *   [optionsProvider]="getStatusOptions"
 *   [value]="selectedStatus()"
 *   placeholder="common.selectStatus"
 *   (valueChange)="selectedStatus.set($event)" />
 * ```
 */
@Component({
  selector: 'app-async-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BrnSelectImports, HlmSelectImports, HlmSpinner, TranslocoPipe],
  template: `
    <brn-select class="block w-full" [value]="value()" [placeholder]="placeholder() | transloco" [attr.aria-label]="label() || (placeholder() | transloco)" (valueChange)="onValueChange($any($event))">
      <hlm-select-trigger class="w-full">
        <hlm-select-value />
      </hlm-select-trigger>

      <hlm-select-content>
        @if (data.isLoading()) {
          <div class="flex items-center justify-center py-6">
            <hlm-spinner size="sm" />
          </div>
        } @else {
          @for (option of data.value() ?? []; track option.value) {
            <hlm-option [value]="option.value">{{ option.label | transloco }}</hlm-option>
          }
        }
      </hlm-select-content>
    </brn-select>
  `,
})
export class AsyncSelectComponent {
  readonly label = input<string>();
  readonly placeholder = input<string>('Select...');
  readonly value = input<string>('');
  readonly optionsProvider = input.required<() => Observable<SelectOptionData[]>>();

  readonly valueChange = output<string>();

  // rxResource: loads options once per optionsProvider, auto-cancels on destroy.
  protected readonly data = rxResource<SelectOptionData[], () => Observable<SelectOptionData[]>>({
    params: () => this.optionsProvider(),
    stream: ({ params: provider }) => provider(),
  });

  protected onValueChange(value: string): void {
    this.valueChange.emit(value);
  }
}
