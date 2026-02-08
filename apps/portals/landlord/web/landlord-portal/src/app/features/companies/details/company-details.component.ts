import { Component, inject, signal, computed, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, of, map, debounceTime, distinctUntilChanged, switchMap, first, Subject, takeUntil, finalize, firstValueFrom } from 'rxjs';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CompanyService } from '../services/company.service';
import { CompanyDetailResponse, CreateCompanyRequest, UpdateCompanyNameRequest } from '../models/company.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonDirective } from '../../../../shared/components/button';
import { IconComponent } from '../../../../shared/components/icon';
import { SpinnerComponent } from '../../../../shared/components/spinner';

@Component({
  selector: 'app-company-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TranslocoPipe,
    ButtonDirective,
    IconComponent,
    SpinnerComponent
  ],
  templateUrl: './company-details.component.html',
  styleUrl: './company-details.component.css'
})
export class CompanyDetailsComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private companyService = inject(CompanyService);
  private dialogService = inject(DialogService);
  private toastService = inject(ToastService);
  private translocoService = inject(TranslocoService);

  private destroy$ = new Subject<void>();

  // State
  company = signal<CompanyDetailResponse | null>(null);
  saving = signal(false);
  loading = signal(false);
  companyId = signal<string>('');

  // Form
  form!: FormGroup;

  get nameControl() {
    return this.form.get('name')!;
  }

  ngOnInit(): void {
    this.route.params.pipe(
      takeUntil(this.destroy$)
    ).subscribe(params => {
      const id = params['id'];
      if (!id) {
        this.router.navigate(['/companies']);
        return;
      }

      this.companyId.set(id);
      this.initializeForm();
      this.loadCompany(id);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required], [this.nameUniqueValidator()]]
    });
  }

  private nameUniqueValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }

      return of(control.value).pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap(name => {
          const excludeId = this.companyId();
          return this.companyService.checkName(name, excludeId);
        }),
        map(response => response.available ? null : { nameTaken: true }),
        first()
      );
    };
  }

  private loadCompany(id: string): void {
    this.loading.set(true);

    this.companyService.get(id).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (company) => {
        if (company) {
          this.company.set(company);
          this.form.patchValue({ name: company.name });
        } else {
          this.toastService.error('companies.error.loadFailed');
          this.router.navigate(['/companies']);
        }
      },
      error: () => {
        this.toastService.error('companies.error.loadFailed');
        this.router.navigate(['/companies']);
      }
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);

    const id = this.companyId();
    const name = this.nameControl.value;
    const request: UpdateCompanyNameRequest = { name };

    this.companyService.update(id, request).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: () => {
        this.toastService.success('companies.success.updated');
        this.form.markAsPristine();

        this.loadCompany(id);
      },
      error: () => {
        this.toastService.error('companies.error.updateFailed');
      }
    });
  }

  cancel(): void {
    if (this.form.dirty) {
      const title = this.translocoService.translate('companies.unsavedChanges');
      const description = this.translocoService.translate('companies.unsavedChangesConfirm');

      this.dialogService.confirm({
        title,
        description,
        variant: 'default'
      }).subscribe(confirmed => {
        if (confirmed) {
          this.router.navigate(['/companies']);
        }
      });
    } else {
      this.router.navigate(['/companies']);
    }
  }

  async deleteCompany(): Promise<void> {
    const company = this.company();
    if (!company) return;

    const title = this.translocoService.translate('common.delete');
    const description = this.translocoService.translate('companies.deleteConfirm', { name: company.name });

    const confirmed = await firstValueFrom(this.dialogService.confirm({
      title,
      description,
      variant: 'destructive'
    }));

    if (!confirmed) return;

    this.loading.set(true);

    this.companyService.delete(company.id).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: () => {
        this.toastService.success('companies.success.deleted');
        this.router.navigate(['/companies']);
      },
      error: () => {
        this.toastService.error('companies.error.deleteFailed');
      }
    });
  }
}
