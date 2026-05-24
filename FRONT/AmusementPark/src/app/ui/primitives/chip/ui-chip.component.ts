import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';

import { UiPrimitiveTone } from '../models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-chip',
  templateUrl: './ui-chip.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiChipComponent {
  @Input() tone: UiPrimitiveTone = 'soft';
  @Input() iconClass: string | null = null;

  @HostBinding('class.app-chip') protected readonly appChipClass: boolean = true;

  @HostBinding('class.app-chip--primary') protected get isPrimary(): boolean {
    return this.tone === 'primary';
  }

  @HostBinding('class.app-chip--ghost') protected get isGhost(): boolean {
    return this.tone === 'ghost';
  }

  @HostBinding('class.app-chip--lime') protected get isLime(): boolean {
    return this.tone === 'lime';
  }

  @HostBinding('class.app-chip--sky') protected get isSky(): boolean {
    return this.tone === 'sky';
  }

  @HostBinding('class.app-chip--rose') protected get isRose(): boolean {
    return this.tone === 'rose';
  }

  @HostBinding('class.app-chip--gold') protected get isGold(): boolean {
    return this.tone === 'gold';
  }

  @HostBinding('class.app-chip--purple') protected get isPurple(): boolean {
    return this.tone === 'purple';
  }

  @HostBinding('class.app-chip--soft') protected get isSoft(): boolean {
    return this.tone === 'soft';
  }
}
