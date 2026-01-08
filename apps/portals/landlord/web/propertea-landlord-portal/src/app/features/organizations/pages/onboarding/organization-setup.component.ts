import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { AuthService } from '@core';

@Component({
  selector: 'app-org-setup',
  standalone: true,
  imports: [CommonModule, ButtonModule, InputTextModule, CardModule],
  template: `
    <div class="flex justify-content-center align-items-center min-h-screen">
      <p-card header="Finalize Your Setup" subheader="Choose your ProperTea URL" styleClass="w-full max-w-25rem">
        <p class="mb-4 text-600">You're almost there! We just need a unique name for your portal.</p>

        <div class="flex flex-column gap-2 mb-4">
          <label for="slug">Organization Slug</label>
          <div class="p-inputgroup">
            <span class="p-inputgroup-addon">propertea.app/</span>
            <input pInputText id="slug" placeholder="acme-corp" />
          </div>
        </div>

        <p-button label="Create Organization" icon="pi pi-check" styleClass="w-full"></p-button>
      </p-card>
    </div>
  `
})
export class OrganizationSetupComponent {
  protected readonly authService = inject(AuthService);
}
