import { ChangeDetectionStrategy, Component, Input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkFitDataQuality,
  ParkFitRecommendationState
} from '@app/models/admin/park-fit/park-fit-data-quality.models';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { AdminParkFitDataQualityFacade } from '../../state/admin-park-fit-data-quality.facade';

@Component({
  selector: 'app-admin-park-fit-operational-controls',
  templateUrl: './admin-park-fit-operational-controls.component.html',
  styleUrl: './admin-park-fit-operational-controls.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslateModule, ButtonDirective]
})
export class AdminParkFitOperationalControlsComponent {
  @Input({ required: true }) public park!: ParkFitDataQuality;

  protected readonly expanded = signal<boolean>(false);
  protected readonly reason = new FormControl<string>('', { nonNullable: true });

  constructor(private readonly facade: AdminParkFitDataQualityFacade) {
  }

  protected get targetState(): ParkFitRecommendationState {
    return this.park.recommendationState === 'Active' ? 'Suspended' : 'Active';
  }

  protected get processing(): boolean {
    return this.facade.isProcessing(`park:${this.park.parkId}`);
  }

  protected toggle(): void {
    this.expanded.update((value: boolean): boolean => !value);
  }

  protected confirm(): void {
    const reason: string = this.reason.value.trim();
    if (reason.length === 0) {
      return;
    }

    this.facade.changeOperationalStatus(this.park, this.targetState, reason);
  }
}
