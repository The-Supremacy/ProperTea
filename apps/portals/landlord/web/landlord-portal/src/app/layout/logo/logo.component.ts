import { Component, input } from '@angular/core';
import { IconComponent } from '../../../shared/components/icon';

@Component({
  selector: 'app-logo',
  imports: [IconComponent],
  template: `
    <div class="inline-flex items-center gap-2">
      <app-icon name="local_cafe" [size]="iconSize()" class="text-primary" />
      @if (showText()) {
        <span [class]="textClass()">ProperTea</span>
      }
    </div>
  `,
  styles: [`
    :host {
      display: inline-flex;
    }
  `]
})
export class LogoComponent {
  size = input<'sm' | 'md' | 'lg'>('md');

  showText = input<boolean>(true);

  iconSize(): number {
    const sizes = { sm: 20, md: 24, lg: 32 };
    return sizes[this.size()];
  }

  textClass(): string {
    const textSizes = { sm: 'text-base', md: 'text-xl', lg: 'text-2xl' };
    return `font-bold ${textSizes[this.size()]}`;
  }
}
