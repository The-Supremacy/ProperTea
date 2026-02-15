import { Directive } from '@angular/core';

/**
 * Directive for consistent drawer footer styling.
 * Applies standard layout and spacing for drawer action buttons.
 *
 * @example
 * ```html
 * <div appDrawerFooter>
 *   <button appBtn variant="outline" (click)="close()">Cancel</button>
 *   <button appBtn type="submit">Save</button>
 * </div>
 * ```
 */
@Directive({
  selector: '[appDrawerFooter]',
  host: {
    class: 'flex gap-2 border-t p-4'
  }
})
export class DrawerFooterDirective {}
