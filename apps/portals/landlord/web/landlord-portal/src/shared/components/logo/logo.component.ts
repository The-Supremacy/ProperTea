import { Component, input, ChangeDetectionStrategy, computed } from '@angular/core';
import { IconComponent } from '../icon';

@Component({
  selector: 'app-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
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

  protected iconSize = computed(() => {
    const sizes = { sm: 20, md: 24, lg: 32 };
    return sizes[this.size()];
  });

  protected textClass = computed(() => {
    const textSizes = { sm: 'text-base', md: 'text-xl', lg: 'text-2xl' };
    return `font-bold ${textSizes[this.size()]}`;
  });
}
