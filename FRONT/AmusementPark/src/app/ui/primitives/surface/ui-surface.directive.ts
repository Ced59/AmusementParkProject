import { Directive, HostBinding, Input } from '@angular/core';

import { UiSurfacePadding, UiSurfaceVariant } from '../models/ui-primitive-variant.model';

@Directive({
  selector: '[appUiSurface]'
})
export class UiSurfaceDirective {
  @Input('appUiSurface') variant: UiSurfaceVariant = 'default';
  @Input() appUiSurfacePadding: UiSurfacePadding = 'regular';

  @HostBinding('class.app-surface') protected readonly appSurfaceClass: boolean = true;

  @HostBinding('class.app-surface--hero') protected get isHero(): boolean {
    return this.variant === 'hero';
  }

  @HostBinding('class.app-surface--panel') protected get isPanel(): boolean {
    return this.variant === 'panel';
  }

  @HostBinding('class.app-surface--filter') protected get isFilter(): boolean {
    return this.variant === 'filter';
  }

  @HostBinding('class.app-surface--shell') protected get isShell(): boolean {
    return this.variant === 'shell';
  }

  @HostBinding('class.app-surface--detail') protected get isDetail(): boolean {
    return this.variant === 'detail';
  }

  @HostBinding('class.app-surface--soft') protected get isSoft(): boolean {
    return this.variant === 'soft';
  }

  @HostBinding('class.app-surface--lift') protected get isLift(): boolean {
    return this.variant === 'lift';
  }

  @HostBinding('class.app-surface--padding-none') protected get hasNoPadding(): boolean {
    return this.appUiSurfacePadding === 'none';
  }

  @HostBinding('class.app-surface--padding-compact') protected get hasCompactPadding(): boolean {
    return this.appUiSurfacePadding === 'compact';
  }

  @HostBinding('class.app-surface--padding-regular') protected get hasRegularPadding(): boolean {
    return this.appUiSurfacePadding === 'regular';
  }

  @HostBinding('class.app-surface--padding-spacious') protected get hasSpaciousPadding(): boolean {
    return this.appUiSurfacePadding === 'spacious';
  }
}
