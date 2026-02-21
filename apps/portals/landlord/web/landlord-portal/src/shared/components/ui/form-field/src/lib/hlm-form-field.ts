import {
	afterRenderEffect,
	ChangeDetectionStrategy,
	Component,
	computed,
	contentChild,
	contentChildren,
	effect,
	ElementRef,
	inject,
	Renderer2,
} from '@angular/core';
import { BrnFormFieldControl } from '@spartan-ng/brain/form-field';
import { classes } from '@spartan-ng/helm/utils';
import { HlmError } from './hlm-error';

let nextFormFieldId = 0;

@Component({
	selector: 'hlm-form-field',
	changeDetection: ChangeDetectionStrategy.OnPush,
	template: `
		<ng-content />

		@switch (_hasDisplayedMessage()) {
			@case ('error') {
				<div [id]="_errorId" role="alert">
					<ng-content select="hlm-error" />
				</div>
			}
			@default {
				<ng-content select="hlm-hint" />
			}
		}
	`,
})
export class HlmFormField {
	private readonly _el = inject(ElementRef);
	private readonly _renderer = inject(Renderer2);

	protected readonly _errorId = `hlm-ff-error-${nextFormFieldId++}`;

	public readonly control = contentChild(BrnFormFieldControl);

	public readonly errorChildren = contentChildren(HlmError);

	protected readonly _hasDisplayedMessage = computed<'error' | 'hint'>(() =>
		this.errorChildren() && this.errorChildren().length > 0 && this.control()?.errorState() ? 'error' : 'hint',
	);

	constructor() {
		classes(() => 'block space-y-2');
		effect(() => {
			if (!this.control()) {
				throw new Error('hlm-form-field must contain a BrnFormFieldControl.');
			}
		});

		afterRenderEffect(() => {
			const showError = this._hasDisplayedMessage() === 'error';
			const input = (this._el.nativeElement as HTMLElement).querySelector('input, textarea, select');
			if (input) {
				if (showError) {
					this._renderer.setAttribute(input, 'aria-describedby', this._errorId);
				} else {
					this._renderer.removeAttribute(input, 'aria-describedby');
				}
			}
		});
	}
}
