import { Directive, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

@Directive({
  selector: 'input[appUppercase]',
  host: {
    '(input)': 'onInput($event)',
  },
})
export class UppercaseInputDirective {
  private readonly ngControl = inject(NgControl, { optional: true });

  protected onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const start = input.selectionStart;
    const end = input.selectionEnd;
    const upper = input.value.toUpperCase();
    input.value = upper;
    if (start !== null && end !== null) {
      input.setSelectionRange(start, end);
    }
    this.ngControl?.control?.setValue(upper, { emitEvent: true });
  }
}
