import { Component, input, output, computed, viewChildren, afterRenderEffect, signal, effect, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { Combobox, ComboboxInput, ComboboxPopup, ComboboxPopupContainer } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable, Subject, finalize, takeUntil } from 'rxjs';
import { IconComponent } from '../icon';
import { SpinnerComponent } from '../spinner';

export interface SelectOptionData {
  value: string;
  label: string;
}

@Component({
  selector: 'app-select',
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
    <div ngCombobox #combobox="ngCombobox" readonly>
      <div #origin class="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2">
        <span class="flex-1">
          @if (displayValue()) {
            <span>{{ displayValue() | transloco }}</span>
          } @else {
            <span class="text-muted-foreground">{{ placeholder() | transloco }}</span>
          }
        </span>
        <input
          ngComboboxInput
          [attr.aria-label]="label() || placeholder()"
          [placeholder]="placeholder() | transloco"
          class="sr-only" />
        <app-icon name="expand_more" [size]="20" />
      </div>

      <ng-template ngComboboxPopupContainer>
        <ng-template
          [cdkConnectedOverlay]="{origin, usePopover: 'inline', matchWidth: true}"
          [cdkConnectedOverlayOpen]="combobox.expanded()">
          <div class="z-50 mt-2 max-h-60 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">
            @if (loading()) {
              <div class="flex items-center justify-center py-6">
                <app-spinner size="sm" />
              </div>
            } @else {
              <div ngListbox [(values)]="selectedValues">
                @for (option of options(); track option.value) {
                  <div
                    ngOption
                    [value]="option.value"
                    [label]="option.label"
                    class="relative flex cursor-pointer select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground data-selected:bg-accent data-selected:text-accent-foreground">
                    <span>{{ option.label | transloco }}</span>
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
export class SelectComponent implements OnInit, OnDestroy {
  label = input<string>();
  placeholder = input<string>('Select...');
  value = input<string>('');
  optionsProvider = input.required<() => Observable<SelectOptionData[]>>();
  valueChange = output<string>();

  // Internal state
  protected options = signal<SelectOptionData[]>([]);
  protected loading = signal(false);
  private destroy$ = new Subject<void>();

  // Track selected values for ngListbox binding
  selectedValues = [this.value()];

  // Get options for scroll-to-active behavior
  protected optionElements = viewChildren(Option);

  protected displayValue = computed(() => {
    const selected = this.options().find(opt => opt.value === this.value());
    return selected?.label || '';
  });

  constructor() {
    // Sync internal selectedValues with input value
    afterRenderEffect(() => {
      this.selectedValues = [this.value()];
    });

    // Emit changes when selection changes
    afterRenderEffect(() => {
      const newValue = this.selectedValues[0];
      if (newValue && newValue !== this.value()) {
        this.valueChange.emit(newValue);
      }
    });

    // Scroll to active option
    afterRenderEffect(() => {
      const option = this.optionElements().find((opt) => opt.active());
      setTimeout(() => option?.element.scrollIntoView({block: 'nearest'}), 50);
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
          console.error('Failed to load select options:', error);
          this.options.set([]);
        },
      });
  }
}
