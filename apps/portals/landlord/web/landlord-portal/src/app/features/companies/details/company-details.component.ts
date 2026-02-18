import { Component, inject, signal, computed, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, AsyncValidatorFn } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, of, map, debounceTime, distinctUntilChanged, switchMap, first, Subject, takeUntil, finalize, firstValueFrom } from 'rxjs';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { CompanyService } from '../services/company.service';
import { CompanyDetailResponse, CreateCompanyRequest, UpdateCompanyRequest } from '../models/company.models';
import { DialogService } from '../../../core/services/dialog.service';
import { ToastService } from '../../../core/services/toast.service';
import { EntityDetailsViewComponent, EntityDetailsConfig } from '../../../../shared/components/entity-details-view';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';
import { HlmInput } from '@spartan-ng/helm/input';
import { CompanyAuditLogComponent } from '../audit-log/company-audit-log.component';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { StatusBadgeDirective } from '../../../../shared/directives';

@Component({
  selector: 'app-company-details',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TranslocoPipe,
    EntityDetailsViewComponent,
    HlmTabsImports,
    HlmInput,
    CompanyAuditLogComponent,
    HlmSpinner,
    StatusBadgeDirective
  ],
  templateUrl: './company-details.component.html'
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
  selectedTab = signal<string>('details');

  // Details view configuration
  detailsConfig = computed<EntityDetailsConfig>(() => {
    const company = this.company();
    return {
      title: this.translocoService.translate('companies.editTitle'),
      subtitle: company?.name,
      showBackButton: true,
      showRefresh: true,
      primaryActions: [
        {
          label: 'common.save',
          icon: 'save',
          variant: 'default',
          handler: () => this.save(),
          disabled: this.form?.invalid || this.saving(),
        },
      ],
      secondaryActions: [
        {
          label: 'common.delete',
          icon: 'delete',
          variant: 'destructive',
          handler: () => this.deleteCompany(),
          separatorBefore: true,
        },
      ],
    };
  });

  // Form
  form!: FormGroup;

  get codeControl() {
    return this.form.get('code')!;
  }

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
      code: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(20)], [this.codeUniqueValidator()]],
      name: ['', [Validators.required], [this.nameUniqueValidator()]]
    });
  }

  private codeUniqueValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }

      return of(control.value).pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap(code => {
          const excludeId = this.companyId();
          return this.companyService.checkCode(code, excludeId);
        }),
        map(response => response.available ? null : { codeTaken: true }),
        first()
      );
    };
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

  protected loadCompany(id: string): void {
    this.loading.set(true);

    this.companyService.get(id).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (company) => {
        if (company) {
          this.company.set(company);
          this.form.patchValue({
            code: company.code,
            name: company.name
          });
          this.form.markAsPristine();
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

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);

    const id = this.companyId();
    const code = this.codeControl.value?.trim().toUpperCase() ?? undefined;
    const name = this.nameControl.value;
    const request: UpdateCompanyRequest = { code, name };

    await firstValueFrom(
      this.companyService.update(id, request).pipe(
        finalize(() => this.saving.set(false))
      )
    ).then(() => {
      this.toastService.success('companies.success.updated');
      this.form.markAsPristine();
      this.loadCompany(id);
    }).catch(() => {
      this.toastService.error('companies.error.updateFailed');
    });
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
