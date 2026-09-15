import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  signal
} from '@angular/core';
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
export class AdminParkFitOperationalControlsComponent implements OnChanges {
  @Input({ required: true }) public park!: ParkFitDataQuality;

  protected readonly selectedTarget = signal<ParkFitRecommendationState | null>(null);
  protected readonly reason = new FormControl<string>('', { nonNullable: true });

  constructor(private readonly facade: AdminParkFitDataQualityFacade) {
  }

  public ngOnChanges(changes: SimpleChanges): void {
    const parkChange = changes['park'];
    const previousPark: ParkFitDataQuality | undefined = parkChange?.previousValue;
    const currentPark: ParkFitDataQuality | undefined = parkChange?.currentValue;
    if (previousPark === undefined || currentPark === undefined) {
      return;
    }

    if (
      previousPark.recommendationState !== currentPark.recommendationState ||
      previousPark.operationalRevision !== currentPark.operationalRevision ||
      previousPark.status !== currentPark.status
    ) {
      this.cancel();
    }
  }

  protected get activationAllowed(): boolean {
    return this.park.status === 'EligibleForFitComparison';
  }

  protected get stateIcon(): string {
    if (this.park.recommendationState === 'Active') {
      return 'pi pi-check-circle';
    }

    return this.park.recommendationState === 'Suspended'
      ? 'pi pi-pause-circle'
      : 'pi pi-circle';
  }

  protected get confirmationSeverity(): 'success' | 'warning' | 'danger' {
    const targetState: ParkFitRecommendationState | null = this.selectedTarget();
    if (targetState === 'Active') {
      return 'success';
    }

    return targetState === 'Suspended' ? 'warning' : 'danger';
  }

  protected get processing(): boolean {
    return this.facade.isProcessing(`park:${this.park.parkId}`);
  }

  protected open(targetState: ParkFitRecommendationState): void {
    this.reason.setValue('');
    this.selectedTarget.set(targetState);
  }

  protected cancel(): void {
    this.selectedTarget.set(null);
    this.reason.setValue('');
  }

  protected actionKey(targetState: ParkFitRecommendationState): string {
    if (targetState === 'Suspended') {
      return 'Suspend';
    }

    if (targetState === 'NotActivated') {
      return 'Deactivate';
    }

    return this.park.recommendationState === 'Suspended' ? 'Restore' : 'Activate';
  }

  protected confirm(): void {
    const reason: string = this.reason.value.trim();
    const targetState: ParkFitRecommendationState | null = this.selectedTarget();
    if (
      reason.length === 0 ||
      targetState === null ||
      (targetState === 'Active' && !this.activationAllowed)
    ) {
      return;
    }

    this.facade.changeOperationalStatus(this.park, targetState, reason);
  }
}
