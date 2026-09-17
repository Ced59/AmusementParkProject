import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, effect, OnInit, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { TripDayPlan, TripParkCandidate, TripParkCandidateState, TripPlan } from '@app/models/trips/trip.models';
import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripCandidateCardComponent } from '../../components/trip-candidate-card/trip-candidate-card.component';
import { TripOverviewStateFacade } from '../../state/trip-overview-state.facade';

interface TripDayDraft {
  candidateId: string;
  arrivalTime: string;
  note: string;
}

@Component({
  selector: 'app-trip-overview-page',
  templateUrl: './trip-overview-page.component.html',
  styleUrl: './trip-overview-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TripOverviewStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    CdkDropList,
    CdkDrag,
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective,
    TripCandidateCardComponent
  ]
})
export class TripOverviewPageComponent implements OnInit {
  protected readonly currentLanguage: string;
  protected readonly startDate = signal<string>('');
  protected readonly endDate = signal<string>('');
  protected readonly selectedWishlistIds = signal<Set<string>>(new Set<string>());
  protected readonly dayDrafts = signal<Record<string, TripDayDraft>>({});
  protected readonly imageWidths: readonly number[] = [120, 200, 320];

  private dateDraftInitialized: boolean = false;

  constructor(
    protected readonly facade: TripOverviewStateFacade,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    translationService: TranslationService
  ) {
    this.currentLanguage = translationService.getCurrentLang() || this.router.url.split('/')[1] || 'en';

    effect((): void => {
      const trip: TripPlan | null = this.facade.trip();
      if (!trip || this.dateDraftInitialized) {
        return;
      }
      this.startDate.set(trip.dateProposal.startDate ?? '');
      this.endDate.set(trip.dateProposal.endDate ?? '');
      this.dateDraftInitialized = true;
    });

    effect((): void => {
      const dates: string[] = this.facade.tripDates();
      const days: TripDayPlan[] = this.facade.program().days;
      const existingDrafts: Record<string, TripDayDraft> = untracked(this.dayDrafts);
      const nextDrafts: Record<string, TripDayDraft> = {};
      for (const date of dates) {
        const saved: TripDayPlan | undefined = days.find((day: TripDayPlan): boolean => day.localDate === date);
        nextDrafts[date] = existingDrafts[date] ?? {
          candidateId: saved?.parkCandidateId ?? '',
          arrivalTime: saved?.desiredArrivalTime ?? '',
          note: saved?.groupNote ?? ''
        };
      }
      this.dayDrafts.set(nextDrafts);
    });
  }

  ngOnInit(): void {
    const tripId: string = this.route.snapshot.paramMap.get('tripId') ?? '';
    this.facade.load(tripId);
  }

  protected saveDates(): void {
    this.facade.setDates(this.startDate(), this.endDate());
  }

  protected toggleWishlist(entryId: string): void {
    this.selectedWishlistIds.update((current: Set<string>): Set<string> => {
      const next: Set<string> = new Set(current);
      if (next.has(entryId)) {
        next.delete(entryId);
      } else {
        next.add(entryId);
      }
      return next;
    });
  }

  protected importSelectedWishlist(): void {
    const selected: Set<string> = this.selectedWishlistIds();
    const entries: UserCollectionEntry[] = this.facade.wishlistParks().filter(
      (entry: UserCollectionEntry): boolean => selected.has(entry.entryId)
    );
    this.facade.importWishlist(entries);
    this.selectedWishlistIds.set(new Set<string>());
  }

  protected dropCandidate(event: CdkDragDrop<TripParkCandidate[]>): void {
    const candidate: TripParkCandidate | undefined = this.facade.program().candidates[event.previousIndex];
    if (candidate) {
      this.facade.moveCandidate(candidate.candidateId, event.currentIndex);
    }
  }

  protected changeCandidateState(candidate: TripParkCandidate, state: TripParkCandidateState): void {
    this.facade.changeCandidateState(candidate, state);
  }

  protected selectedCandidates(): TripParkCandidate[] {
    return this.facade.program().candidates.filter(
      (candidate: TripParkCandidate): boolean => candidate.state === 'Selected' && candidate.isParkAvailable
    );
  }

  protected updateDay(date: string, field: keyof TripDayDraft, value: string): void {
    this.dayDrafts.update((drafts: Record<string, TripDayDraft>): Record<string, TripDayDraft> => ({
      ...drafts,
      [date]: {
        ...(drafts[date] ?? { candidateId: '', arrivalTime: '', note: '' }),
        [field]: value
      }
    }));
  }

  protected saveDay(date: string): void {
    const draft: TripDayDraft | undefined = this.dayDrafts()[date];
    if (draft) {
      this.facade.saveDay(date, draft.candidateId, draft.arrivalTime, draft.note);
    }
  }

  protected stateLabelKey(state: TripParkCandidateState): string {
    return `trips.candidates.states.${state.toLowerCase()}`;
  }
}
