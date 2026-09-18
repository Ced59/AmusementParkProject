import {
  ChangeDetectionStrategy,
  Component,
  effect,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  signal,
  SimpleChanges
} from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import {
  TripDelegatedRole,
  TripEffectiveRole,
  TripParticipant
} from '@app/models/trips/trip-participant.models';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripParticipantsStateFacade } from '../../state/trip-participants-state.facade';

@Component({
  selector: 'app-trip-participant-panel',
  templateUrl: './trip-participant-panel.component.html',
  styleUrl: './trip-participant-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripParticipantsStateFacade],
  imports: [TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class TripParticipantPanelComponent implements OnChanges {
  @Input({ required: true }) tripPlanId: string = '';
  @Output() readonly left: EventEmitter<void> = new EventEmitter<void>();

  protected readonly transferCandidateId = signal<string | null>(null);
  protected readonly previousOwnerRole = signal<TripDelegatedRole>('Editor');
  protected readonly confirmingLeave = signal<boolean>(false);
  protected readonly delegatedRoles: readonly TripDelegatedRole[] = ['Editor', 'Participant', 'Viewer'];

  constructor(protected readonly facade: TripParticipantsStateFacade) {
    effect((): void => {
      if (this.facade.left()) {
        this.left.emit();
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tripPlanId']) {
      this.facade.load(this.tripPlanId);
    }
  }

  protected roleLabelKey(role: TripEffectiveRole): string {
    return `trips.participants.roles.${role.toLowerCase()}`;
  }

  protected initials(displayName: string): string {
    return displayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part: string): string => part[0]?.toLocaleUpperCase() ?? '')
      .join('') || '•';
  }

  protected changeRole(participant: TripParticipant, role: string): void {
    if (role === 'Editor' || role === 'Participant' || role === 'Viewer') {
      this.facade.changeRole(participant.memberId, role);
    }
  }

  protected beginTransfer(participant: TripParticipant): void {
    this.transferCandidateId.set(participant.memberId);
  }

  protected cancelTransfer(): void {
    this.transferCandidateId.set(null);
  }

  protected confirmTransfer(participant: TripParticipant): void {
    this.facade.transferOwnership(participant.memberId, this.previousOwnerRole());
    this.transferCandidateId.set(null);
  }

  protected confirmLeave(): void {
    this.facade.leave();
    this.confirmingLeave.set(false);
  }
}
