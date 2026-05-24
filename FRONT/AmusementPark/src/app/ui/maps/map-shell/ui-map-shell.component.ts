import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiKickerComponent } from '@ui/primitives';

@Component({
  selector: 'app-ui-map-shell',
  templateUrl: './ui-map-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiKickerComponent]
})
export class UiMapShellComponent {
  @Input() kickerLabelKey: string | null = null;
  @Input() kickerText: string | null = null;
  @Input() kickerIconClass: string = 'pi pi-map';
  @Input() titleKey: string | null = null;
  @Input() titleText: string | null = null;
  @Input() subtitleKey: string | null = null;
  @Input() subtitleText: string | null = null;
  @Input() compact: boolean = false;

  protected get hasHeader(): boolean {
    return !!this.kickerLabelKey || !!this.kickerText || !!this.titleKey || !!this.titleText || !!this.subtitleKey || !!this.subtitleText;
  }

  protected get hasKicker(): boolean {
    return !!this.kickerLabelKey || !!this.kickerText;
  }

  protected get hasTitle(): boolean {
    return !!this.titleKey || !!this.titleText;
  }

  protected get hasSubtitle(): boolean {
    return !!this.subtitleKey || !!this.subtitleText;
  }
}
