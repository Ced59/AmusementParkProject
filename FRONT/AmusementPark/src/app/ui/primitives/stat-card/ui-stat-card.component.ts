import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiPrimitiveTone } from '../models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-stat-card',
  templateUrl: './ui-stat-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class UiStatCardComponent {
  @Input() labelKey: string | null = null;
  @Input() labelText: string | null = null;
  @Input() value: string | number | null = null;
  @Input() hintKey: string | null = null;
  @Input() hintText: string | null = null;
  @Input() tone: UiPrimitiveTone = 'soft';

  @HostBinding('class.app-stat-card') protected readonly appStatCardClass: boolean = true;
  @HostBinding('class.app-stat-card--accent') protected get isAccent(): boolean {
    return this.tone === 'primary';
  }

  @HostBinding('class.app-stat-card--lime') protected get isLime(): boolean {
    return this.tone === 'lime';
  }

  @HostBinding('class.app-stat-card--sky') protected get isSky(): boolean {
    return this.tone === 'sky';
  }

  @HostBinding('class.app-stat-card--rose') protected get isRose(): boolean {
    return this.tone === 'rose';
  }

  @HostBinding('class.app-stat-card--gold') protected get isGold(): boolean {
    return this.tone === 'gold';
  }

  @HostBinding('class.app-stat-card--purple') protected get isPurple(): boolean {
    return this.tone === 'purple';
  }

  protected hasHint(): boolean {
    return !!this.hintKey || !!this.hintText;
  }
}
