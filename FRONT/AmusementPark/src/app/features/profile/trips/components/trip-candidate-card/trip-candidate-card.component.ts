import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { TranslateModule } from '@ngx-translate/core';

import { TripParkCandidate, TripParkCandidateState } from '@app/models/trips/trip.models';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiButtonDirective, UiChipComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-trip-candidate-card',
  templateUrl: './trip-candidate-card.component.html',
  styleUrl: './trip-candidate-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkDragHandle, TranslateModule, ImageDisplayComponent, UiButtonDirective, UiChipComponent, UiSurfaceDirective]
})
export class TripCandidateCardComponent {
  @Input({ required: true }) candidate!: TripParkCandidate;
  @Input() imageId: string | null = null;
  @Input() index: number = 0;
  @Input() count: number = 0;
  @Input() disabled: boolean = false;
  @Input() scheduled: boolean = false;

  @Output() stateChanged = new EventEmitter<TripParkCandidateState>();
  @Output() moveRequested = new EventEmitter<number>();

  protected readonly imageWidths: readonly number[] = [120, 200, 320];
  protected readonly states: readonly TripParkCandidateState[] = [
    'Proposed',
    'Shortlisted',
    'Selected',
    'Rejected'
  ];

  protected stateLabelKey(state: TripParkCandidateState): string {
    return `trips.candidates.states.${state.toLowerCase()}`;
  }

  protected isStateDisabled(state: TripParkCandidateState): boolean {
    return this.disabled
      || this.candidate.state === state
      || (this.scheduled && state !== 'Selected');
  }
}
