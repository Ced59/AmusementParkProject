import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiDistanceMetricModel } from '../models/ui-distance-panel.model';

@Component({
  selector: 'app-ui-distance-panel',
  templateUrl: './ui-distance-panel.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class UiDistancePanelComponent {
  @Input() titleKey: string = 'maps.distance.title';
  @Input() titleText: string | null = null;
  @Input() sourceLabel: string | null = null;
  @Input() targetLabel: string | null = null;
  @Input() distanceValue: string | number | null = null;
  @Input() distanceUnit: string | null = null;
  @Input() durationText: string | null = null;
  @Input() noteKey: string | null = null;
  @Input() noteText: string | null = null;
  @Input() metrics: UiDistanceMetricModel[] = [];

  protected get hasDistance(): boolean {
    return this.distanceValue !== null && this.distanceValue !== undefined && this.distanceValue !== '';
  }

  protected get hasRouteLabels(): boolean {
    return !!this.sourceLabel || !!this.targetLabel;
  }

  protected get hasNote(): boolean {
    return !!this.noteKey || !!this.noteText;
  }

  protected get hasMetrics(): boolean {
    return this.metrics.length > 0;
  }
}
