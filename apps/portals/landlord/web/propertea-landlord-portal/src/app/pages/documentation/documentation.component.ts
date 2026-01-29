import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'app-documentation',
  imports: [ButtonModule, CardModule, RouterLink, TranslocoModule],
  templateUrl: './documentation.component.html',
  styleUrl: './documentation.component.scss'
})
export class DocumentationComponent {}
