import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmFormFieldImports } from '@spartan-ng/helm/form-field';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';

let nextAddressId = 0;

@Component({
  selector: 'app-address-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslocoPipe, HlmFormFieldImports, HlmInput, HlmLabel],
  template: `
    <div class="space-y-4" [formGroup]="addressGroup()">
      <hlm-form-field>
        <label hlmLabel [for]="streetId">
          {{ 'address.streetAddress' | transloco }}
          @if (required()) {
            <span class="text-destructive">*</span>
          }
        </label>
        <input hlmInput [id]="streetId" formControlName="streetAddress" />
        @if (required() && streetAddrCtrl()?.touched && streetAddrCtrl()?.hasError('required')) {
          <hlm-error>{{ 'address.streetAddressRequired' | transloco }}</hlm-error>
        }
      </hlm-form-field>

      <div class="grid grid-cols-3 gap-4">
        <hlm-form-field>
          <label hlmLabel [for]="countryId">
            {{ 'address.country' | transloco }}
            @if (required()) {
              <span class="text-destructive">*</span>
            }
          </label>
          <input hlmInput [id]="countryId" formControlName="country" />
          @if (required() && countryCtrl()?.touched && countryCtrl()?.hasError('required')) {
            <hlm-error>{{ 'address.countryRequired' | transloco }}</hlm-error>
          }
        </hlm-form-field>

        <hlm-form-field>
          <label hlmLabel [for]="cityId">
            {{ 'address.city' | transloco }}
            @if (required()) {
              <span class="text-destructive">*</span>
            }
          </label>
          <input hlmInput [id]="cityId" formControlName="city" />
          @if (required() && cityCtrl()?.touched && cityCtrl()?.hasError('required')) {
            <hlm-error>{{ 'address.cityRequired' | transloco }}</hlm-error>
          }
        </hlm-form-field>

        <hlm-form-field>
          <label hlmLabel [for]="zipId">
            {{ 'address.zipCode' | transloco }}
            @if (required()) {
              <span class="text-destructive">*</span>
            }
          </label>
          <input hlmInput [id]="zipId" formControlName="zipCode" />
          @if (required() && zipCtrl()?.touched && zipCtrl()?.hasError('required')) {
            <hlm-error>{{ 'address.zipCodeRequired' | transloco }}</hlm-error>
          }
        </hlm-form-field>
      </div>
    </div>
  `,
})
export class AddressFormComponent {
  addressGroup = input.required<FormGroup>();
  required = input<boolean>(false);

  private readonly _uid = nextAddressId++;
  protected readonly streetId = `address-street-${this._uid}`;
  protected readonly countryId = `address-country-${this._uid}`;
  protected readonly cityId = `address-city-${this._uid}`;
  protected readonly zipId = `address-zip-${this._uid}`;

  protected readonly streetAddrCtrl = computed(() => this.addressGroup().get('streetAddress'));
  protected readonly countryCtrl = computed(() => this.addressGroup().get('country'));
  protected readonly cityCtrl = computed(() => this.addressGroup().get('city'));
  protected readonly zipCtrl = computed(() => this.addressGroup().get('zipCode'));
}
