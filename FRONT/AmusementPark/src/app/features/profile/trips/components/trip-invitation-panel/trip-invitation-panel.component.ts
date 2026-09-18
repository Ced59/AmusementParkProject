import { DOCUMENT, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, Input, OnChanges, SimpleChanges, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  TripInvitationMemberCountBand,
  TripInvitationRole,
  TripInvitationSummary
} from '@app/models/trips/trip-invitation.models';
import { TripPlan } from '@app/models/trips/trip.models';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripInvitationsStateFacade } from '../../state/trip-invitations-state.facade';

@Component({
  selector: 'app-trip-invitation-panel',
  templateUrl: './trip-invitation-panel.component.html',
  styleUrl: './trip-invitation-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripInvitationsStateFacade],
  imports: [
    DatePipe,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class TripInvitationPanelComponent implements OnChanges {
  @Input({ required: true }) trip!: TripPlan;
  @Input({ required: true }) currentLanguage: string = 'en';

  protected readonly selectedRole = signal<TripInvitationRole>('Participant');
  protected readonly lifetimeHours = signal<number>(168);
  protected readonly targetEmail = signal<string>('');
  protected readonly copied = signal<boolean>(false);

  constructor(
    protected readonly facade: TripInvitationsStateFacade,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['trip'] && this.trip?.tripPlanId) {
      this.facade.load(this.trip.tripPlanId, this.trip.version);
    }
  }

  protected createInvitation(): void {
    if (this.targetEmail() && !this.isTargetEmailValid()) {
      return;
    }
    this.copied.set(false);
    this.facade.create(this.selectedRole(), this.lifetimeHours(), this.targetEmail());
  }

  protected revoke(invitation: TripInvitationSummary): void {
    this.facade.revoke(invitation);
  }

  protected invitationPath(): string[] {
    const token: string = this.facade.creation()?.token ?? '';
    return ['/', this.currentLanguage, 'trip-invitations', token];
  }

  protected invitationUrl(): string {
    const token: string = this.facade.creation()?.token ?? '';
    if (!token) {
      return '';
    }
    const encodedLanguage: string = encodeURIComponent(this.currentLanguage);
    const encodedToken: string = encodeURIComponent(token);
    const path: string = `/${encodedLanguage}/trip-invitations/${encodedToken}`;
    const origin: string = this.document.location?.origin ?? '';
    return `${origin}${path}`;
  }

  protected copyInvitation(): void {
    const url: string = this.invitationUrl();
    if (!url || !globalThis.navigator?.clipboard) {
      return;
    }
    globalThis.navigator.clipboard.writeText(url)
      .then((): void => this.copied.set(true))
      .catch((): void => this.copied.set(false));
  }

  protected isTargetEmailValid(): boolean {
    const value: string = this.targetEmail().trim();
    return !value || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
  }

  protected previewMemberBand(): TripInvitationMemberCountBand {
    const count: number = this.trip.memberCount;
    if (count <= 1) {
      return 'One';
    }
    if (count <= 5) {
      return 'TwoToFive';
    }
    if (count <= 10) {
      return 'SixToTen';
    }
    return 'ElevenToFifty';
  }

  protected previewMonths(): string[] {
    const dates: string[] = this.trip.dateProposal.kind === 'Candidates'
      ? this.trip.dateProposal.candidateDates
      : [this.trip.dateProposal.startDate, this.trip.dateProposal.endDate]
        .filter((date: string | null): date is string => !!date);
    if (dates.length === 0) {
      return [];
    }
    const months: string[] = dates.map((date: string): string => date.slice(0, 7)).sort();
    return months[0] === months[months.length - 1]
      ? [months[0]]
      : [months[0], months[months.length - 1]];
  }

  protected roleLabelKey(role: TripInvitationRole): string {
    return `trips.invitations.roles.${role.toLowerCase()}`;
  }

  protected memberBandLabelKey(band: TripInvitationMemberCountBand): string {
    return `trips.invitations.memberBands.${band.toLowerCase()}`;
  }
}
