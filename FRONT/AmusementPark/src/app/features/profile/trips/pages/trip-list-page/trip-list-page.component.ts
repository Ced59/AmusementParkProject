import { ChangeDetectionStrategy, Component, effect, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { TripPlan } from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { areTripDateInputsValid, resolvedBrowserTimeZone } from '../../state/trip-date-proposal.helpers';
import { TripListStateFacade } from '../../state/trip-list-state.facade';

@Component({
  selector: 'app-trip-list-page',
  templateUrl: './trip-list-page.component.html',
  styleUrl: './trip-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripListStateFacade],
  imports: [RouterLink, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class TripListPageComponent implements OnInit {
  protected readonly title = signal<string>('');
  protected readonly startDate = signal<string>('');
  protected readonly endDate = signal<string>('');
  protected readonly destinationTimeZoneId = signal<string>(resolvedBrowserTimeZone());
  protected readonly composerVisible = signal<boolean>(false);

  constructor(
    protected readonly facade: TripListStateFacade,
    private readonly router: Router,
    protected readonly translationService: TranslationService
  ) {
    effect((): void => {
      const tripId: string | null = this.facade.createdTripId();
      if (!tripId) {
        return;
      }
      this.facade.clearCreatedTrip();
      void this.router.navigate(['/', this.currentLanguage(), 'profile', 'trips', tripId]);
    });
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected create(): void {
    if (this.canCreate()) {
      this.facade.create(this.title(), this.startDate(), this.endDate(), this.destinationTimeZoneId());
    }
  }

  protected canCreate(): boolean {
    return !!this.title().trim()
      && areTripDateInputsValid(this.startDate(), this.endDate())
      && (!this.startDate() || !!this.destinationTimeZoneId().trim());
  }

  protected datesInvalid(): boolean {
    return !areTripDateInputsValid(this.startDate(), this.endDate());
  }

  protected open(trip: TripPlan): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile', 'trips', trip.tripPlanId]);
  }

  protected dateLabelKey(trip: TripPlan): string {
    return `trips.dates.kinds.${trip.dateProposal.kind.toLowerCase()}`;
  }

  protected dateRange(trip: TripPlan): string {
    if (trip.dateProposal.kind === 'Candidates') {
      return trip.dateProposal.candidateDates.join(' · ');
    }
    if (!trip.dateProposal.startDate) {
      return '';
    }
    return trip.dateProposal.endDate && trip.dateProposal.endDate !== trip.dateProposal.startDate
      ? `${trip.dateProposal.startDate} → ${trip.dateProposal.endDate}`
      : trip.dateProposal.startDate;
  }

  private currentLanguage(): string {
    return this.translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';
  }
}
