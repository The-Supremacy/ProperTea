import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  template: `
    <mat-icon
      [fontIcon]="name()"
      [style.fontSize.px]="size()"
      [style.width.px]="size()"
      [style.height.px]="size()">
    </mat-icon>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    mat-icon {
      width: auto !important;
      height: auto !important;
      font-size: inherit;
    }
  `]
})
export class IconComponent {
  name = input.required<string>();
  size = input<number>(24);
}

