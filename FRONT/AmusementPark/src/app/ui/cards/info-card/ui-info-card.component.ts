import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { SafeExternalUrlPipe } from '@shared/pipes';
import { UiButtonDirective, UiKickerComponent } from '@ui/primitives';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';
import { UiCardActionModel, UiInfoCardMetricModel } from '../models/ui-card-action.model';

@Component({
  selector: 'app-ui-info-card',
  templateUrl: './ui-info-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, SafeExternalUrlPipe, UiButtonDirective, UiKickerComponent]
})
export class UiInfoCardComponent {
  @Input() kickerLabelKey: string | null = null;
  @Input() kickerText: string | null = null;
  @Input() kickerIconClass: string | null = null;
  @Input() kickerTone: UiPrimitiveTone = 'soft';
  @Input() titleKey: string | null = null;
  @Input() titleText: string | null = null;
  @Input() textKey: string | null = null;
  @Input() text: string | null = null;
  @Input() metrics: UiInfoCardMetricModel[] = [];
  @Input() primaryAction: UiCardActionModel | null = null;
  @Input() secondaryAction: UiCardActionModel | null = null;
  @Input() compact: boolean = false;

  protected get hasKicker(): boolean {
    return !!this.kickerLabelKey || !!this.kickerText || !!this.kickerIconClass;
  }

  protected get hasTitle(): boolean {
    return !!this.titleKey || !!this.titleText;
  }

  protected get hasText(): boolean {
    return !!this.textKey || !!this.text;
  }

  protected get hasMetrics(): boolean {
    return this.metrics.length > 0;
  }
}
