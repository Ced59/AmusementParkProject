import { Directive, HostBinding, Input } from '@angular/core';

import { UiPrimitiveTone } from '../models/ui-primitive-variant.model';

@Directive({
  selector: 'a[appUiButton], button[appUiButton]'
})
export class UiButtonDirective {
  @Input('appUiButton') variant: UiPrimitiveTone = 'primary';
  @Input() appUiButtonFull: boolean = false;
  @Input() appUiButtonMobileFull: boolean = false;
  @Input() appUiButtonDisabled: boolean = false;

  @HostBinding('class.app-button') protected readonly appButtonClass: boolean = true;
  @HostBinding('attr.aria-disabled') protected get ariaDisabled(): string | null {
    return this.appUiButtonDisabled ? 'true' : null;
  }

  @HostBinding('class.app-button--primary') protected get isPrimary(): boolean {
    return this.variant === 'primary';
  }

  @HostBinding('class.app-button--ghost') protected get isGhost(): boolean {
    return this.variant === 'ghost';
  }

  @HostBinding('class.app-button--lime') protected get isLime(): boolean {
    return this.variant === 'lime';
  }

  @HostBinding('class.app-button--sky') protected get isSky(): boolean {
    return this.variant === 'sky';
  }

  @HostBinding('class.app-button--rose') protected get isRose(): boolean {
    return this.variant === 'rose';
  }

  @HostBinding('class.app-button--gold') protected get isGold(): boolean {
    return this.variant === 'gold';
  }

  @HostBinding('class.app-button--purple') protected get isPurple(): boolean {
    return this.variant === 'purple';
  }

  @HostBinding('class.app-button--soft') protected get isSoft(): boolean {
    return this.variant === 'soft';
  }

  @HostBinding('class.app-button--full') protected get isFull(): boolean {
    return this.appUiButtonFull;
  }

  @HostBinding('class.app-button--mobile-full') protected get isMobileFull(): boolean {
    return this.appUiButtonMobileFull;
  }

  @HostBinding('class.app-button--disabled') protected get isDisabled(): boolean {
    return this.appUiButtonDisabled;
  }
}
