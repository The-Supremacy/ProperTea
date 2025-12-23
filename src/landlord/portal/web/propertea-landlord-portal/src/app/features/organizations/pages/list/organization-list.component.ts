import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-org-list ',
  standalone: true,
  imports: [CommonModule, ButtonModule, InputTextModule, CardModule],
  template: `
    <div>
    </div>
  `
})
export class OrganizationListComponent {
}
