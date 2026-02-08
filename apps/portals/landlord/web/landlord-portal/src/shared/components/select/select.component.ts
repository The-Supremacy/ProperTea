import { Component, input, output, computed, viewChildren, afterRenderEffect, ChangeDetectionStrategy } from '@angular/core';
import { Combobox, ComboboxInput, ComboboxPopup, ComboboxPopupContainer } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import { TranslocoPipe } from '@jsverse/transloco';
import { IconComponent } from '../icon';

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
  ],
  template: `
    <div ngCombobox readonly>
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
          [cdkConnectedOverlayOpen]="true">
          <div class="z-50 mt-2 max-h-60 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">
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
          </div>
        </ng-template>
      </ng-template>
    </div>
  `,
})
export class SelectComponent {
  label = input<string>();
  placeholder = input<string>('Select...');
  value = input<string>('');
  options = input.required<SelectOptionData[]>();
  valueChange = output<string>();

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
}
