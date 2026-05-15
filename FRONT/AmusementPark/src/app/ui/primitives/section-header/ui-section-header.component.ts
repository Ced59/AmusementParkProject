import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiKickerComponent } from '../kicker/ui-kicker.component';
import { UiPrimitiveTone } from '../models/ui-primitive-variant.model';

@Component({
  selector: 'app-ui-section-header',
  templateUrl: './ui-section-header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiKickerComponent]
})
export class UiSectionHeaderComponent {
  @Input() kickerIconClass: string | null = null;
  @Input() kickerLabelKey: string | null = null;
  @Input() kickerText: string | null = null;
  @Input() kickerTone: UiPrimitiveTone = 'primary';
  @Input() titleKey: string | null = null;
  @Input() titleText: string | null = null;
  @Input() subtitleKey: string | null = null;
  @Input() subtitleText: string | null = null;

  @HostBinding('class.app-section-header') protected readonly appSectionHeaderClass: boolean = true;

  protected hasKicker(): boolean {
    return !!this.kickerIconClass || !!this.kickerLabelKey || !!this.kickerText;
  }

  protected hasProjectedTitle(): boolean {
    return !this.titleKey && !this.titleText;
  }

  protected hasSubtitle(): boolean {
    return !!this.subtitleKey || !!this.subtitleText;
  }
}
