import {
  Component,
  input,
  output,
  signal,
  computed,
  effect,
  untracked,
  viewChild,
  viewChildren,
  afterRenderEffect,
  ChangeDetectionStrategy,
} from '@angular/core';
import { Combobox, ComboboxInput, ComboboxPopupContainer } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { SpinnerComponent } from '../spinner/spinner.component';
import { IconComponent } from '../icon';

export interface AsyncSelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-async-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Combobox,
    ComboboxInput,
    ComboboxPopupContainer,
    Listbox,
    Option,
    OverlayModule,
    FormsModule,
    TranslocoPipe,
    SpinnerComponent,
    IconComponent,
  ],
  template: `
    <div ngCombobox filterMode="auto-select">
      <div
        #origin
        class="flex h-10 w-full items-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2"
        [class.opacity-50]="disabled()"
      >
        <input
          ngComboboxInput
          [(ngModel)]="query"
          [attr.aria-label]="label() || placeholder()"
          [placeholder]="placeholder() | transloco"
          [disabled]="disabled()"
          class="min-w-0 flex-1 bg-transparent outline-none placeholder:text-muted-foreground"
        />
        @if (loading()) {
          <app-spinner size="sm" />
        } @else {
          <app-icon name="expand_more" [size]="20" class="text-muted-foreground" />
        }
      </div>

      <ng-template ngComboboxPopupContainer>
        <ng-template
          [cdkConnectedOverlay]="{ origin, usePopover: 'inline', matchWidth: true }"
          [cdkConnectedOverlayOpen]="true"
        >
          <div
            class="z-50 mt-1 max-h-60 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md"
          >
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
              <div ngListbox>
                @for (option of filteredOptions(); track option.value) {
                  <div
                    ngOption
                    [value]="option.value"
                    [label]="option.label"
                    class="relative flex cursor-pointer select-none items-center px-3 py-2 text-sm outline-none hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground data-selected:bg-accent data-selected:font-medium"
                  >
                    <span class="truncate">{{ option.label }}</span>
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
  label = input<string>();
  placeholder = input('common.select');
  emptyMessage = input('common.noResults');
  disabled = input(false);
  loading = input(false);
  options = input<AsyncSelectOption[]>([]);
  value = input<string>('');
  valueChange = output<string>();
  opened = output<void>();

  protected query = signal('');

  private combobox = viewChild(Combobox);
  private listbox = viewChild(Listbox);
  private optionElements = viewChildren(Option);
  private wasExpanded = false;
  private lastEmittedValue = '';

  protected filteredOptions = computed(() => {
    const all = this.options();
    const q = this.query().trim().toLowerCase();
    if (!q) return all;

    const selected = all.find((o) => o.value === this.value());
    if (selected && q === selected.label.toLowerCase()) return all;

    return all.filter((opt) => opt.label.toLowerCase().includes(q));
  });

  constructor() {
    // Sync external value input -> query signal for pre-selection display
    effect(() => {
      const v = this.value();
      const opts = this.options();
      untracked(() => {
        if (!v) {
          this.query.set('');
          this.lastEmittedValue = '';
        } else {
          const match = opts.find((o) => o.value === v);
          if (match) {
            this.query.set(match.label);
            this.lastEmittedValue = v;
          }
        }
      });
    });

    // Detect open/close transitions AFTER render settles.
    // Using afterRenderEffect (not effect) ensures we never interfere
    // with the combobox's own internal close/select logic.
    afterRenderEffect(() => {
      const expanded = this.combobox()?.expanded() ?? false;

      if (!this.wasExpanded && expanded) {
        this.opened.emit();
      }

      if (this.wasExpanded && !expanded) {
        const values = this.listbox()?.values() ?? [];
        const selected = (values[0] as string) ?? '';
        if (selected && selected !== this.lastEmittedValue) {
          this.lastEmittedValue = selected;
          this.valueChange.emit(selected);
        }
      }

      this.wasExpanded = expanded;
    });

    // Scrolls to the active item when the active option changes
    afterRenderEffect(() => {
      const option = this.optionElements().find((opt) => opt.active());
      setTimeout(() => option?.element.scrollIntoView({ block: 'nearest' }), 50);
    });

    // Resets the listbox scroll position when the combobox is closed
    afterRenderEffect(() => {
      if (!this.combobox()?.expanded()) {
        setTimeout(() => this.listbox()?.element.scrollTo(0, 0), 150);
      }
    });
  }
}
