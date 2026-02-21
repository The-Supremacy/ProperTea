import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';
import { UppercaseInputDirective } from '../../../../shared/directives';
import { BuildingService } from '../services/building.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-create-entrance-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    TranslocoPipe,
    HlmInput,
    HlmFormFieldImports,
    HlmLabel,
    HlmButton,
    HlmSpinner,
    HlmSheetImports,
    UppercaseInputDirective,
  ],
  template: `
    <hlm-sheet side="right" [state]="open() ? 'open' : 'closed'" (closed)="onSheetClosed()">
      <hlm-sheet-content *hlmSheetPortal="let ctx" class="sm:max-w-md flex flex-col">
        <hlm-sheet-header>
          <h3 hlmSheetTitle>{{ 'buildings.entrance.new' | transloco }}</h3>
          <p hlmSheetDescription>{{ 'buildings.entrance.createHint' | transloco }}</p>
        </hlm-sheet-header>

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex min-h-0 flex-1 flex-col">
          <div class="flex-1 space-y-6 overflow-y-auto px-4">

            <hlm-form-field>
              <label hlmLabel for="entrance-code">
                {{ 'buildings.code' | transloco }}
                <span class="text-destructive">*</span>
              </label>
              <input
                hlmInput
                id="entrance-code"
                appUppercase
                type="text"
                formControlName="code"
                class="w-full font-mono"
                [placeholder]="'buildings.entrance.codePlaceholder' | transloco" />
              @if (form.controls.code.invalid && form.controls.code.touched) {
                @if (form.controls.code.hasError('required')) {
                  <hlm-error>{{ 'buildings.entrance.codeRequired' | transloco }}</hlm-error>
                }
                @if (form.controls.code.hasError('maxlength')) {
                  <hlm-error>{{ 'buildings.entrance.codeTooLong' | transloco }}</hlm-error>
                }
              }
            </hlm-form-field>

            <hlm-form-field>
              <label hlmLabel for="entrance-name">
                {{ 'buildings.name' | transloco }}
                <span class="text-destructive">*</span>
              </label>
              <input
                hlmInput
                id="entrance-name"
                type="text"
                formControlName="name"
                class="w-full"
                [placeholder]="'buildings.entrance.namePlaceholder' | transloco" />
              @if (form.controls.name.invalid && form.controls.name.touched) {
                @if (form.controls.name.hasError('required')) {
                  <hlm-error>{{ 'buildings.entrance.nameRequired' | transloco }}</hlm-error>
                }
                @if (form.controls.name.hasError('maxlength')) {
                  <hlm-error>{{ 'buildings.entrance.nameTooLong' | transloco }}</hlm-error>
                }
              }
            </hlm-form-field>
          </div>

          <hlm-sheet-footer>
            <button
              type="button"
              hlmBtn
              variant="outline"
              class="flex-1"
              hlmSheetClose
              [disabled]="isSubmitting()">
              {{ 'common.cancel' | transloco }}
            </button>
            <button
              type="submit"
              hlmBtn
              class="flex-1"
              [disabled]="!canSubmit()">
              @if (isSubmitting()) {
                <hlm-spinner size="sm" />
              }
              {{ 'common.create' | transloco }}
            </button>
          </hlm-sheet-footer>
        </form>
      </hlm-sheet-content>
    </hlm-sheet>
  `,
})
export class CreateEntranceDrawerComponent {
  private fb = inject(FormBuilder);
  private buildingService = inject(BuildingService);
  private toastService = inject(ToastService);

  open = input.required<boolean>();
  buildingId = input.required<string>();

  openChange = output<boolean>();
  created = output<void>();

  protected isSubmitting = signal(false);

  protected form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(5), Validators.pattern(/^[A-Z0-9]*$/)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  private formStatus = toSignal(this.form.statusChanges, { initialValue: this.form.status });
  protected canSubmit = computed(() => this.formStatus() === 'VALID' && !this.isSubmitting());

  protected onSheetClosed(): void {
    this.form.reset();
    this.openChange.emit(false);
  }

  protected submit(): void {
    if (!this.canSubmit()) return;

    this.isSubmitting.set(true);

    this.buildingService
      .addEntrance(this.buildingId(), {
        code: this.form.controls.code.value.trim().toUpperCase(),
        name: this.form.controls.name.value.trim(),
      })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.toastService.success('buildings.entrance.createSuccess');
          this.form.reset();
          this.openChange.emit(false);
          this.created.emit();
        },
        error: () => {
          // Global error interceptor shows the toast.
        },
      });
  }
}
