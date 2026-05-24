import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';

import { UiPrimitiveTone } from '../models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-kicker',
  templateUrl: './ui-kicker.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiKickerComponent {
  @Input() tone: UiPrimitiveTone = 'primary';
  @Input() iconClass: string | null = null;

  @HostBinding('class.app-kicker') protected readonly appKickerClass: boolean = true;

  @HostBinding('class.app-kicker--primary') protected get isPrimary(): boolean {
    return this.tone === 'primary';
  }

  @HostBinding('class.app-kicker--ghost') protected get isGhost(): boolean {
    return this.tone === 'ghost';
  }

  @HostBinding('class.app-kicker--lime') protected get isLime(): boolean {
    return this.tone === 'lime';
  }

  @HostBinding('class.app-kicker--sky') protected get isSky(): boolean {
    return this.tone === 'sky';
  }

  @HostBinding('class.app-kicker--rose') protected get isRose(): boolean {
    return this.tone === 'rose';
  }

  @HostBinding('class.app-kicker--gold') protected get isGold(): boolean {
    return this.tone === 'gold';
  }

  @HostBinding('class.app-kicker--purple') protected get isPurple(): boolean {
    return this.tone === 'purple';
  }

  @HostBinding('class.app-kicker--soft') protected get isSoft(): boolean {
    return this.tone === 'soft';
  }
}
