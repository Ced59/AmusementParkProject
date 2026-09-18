import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, DestroyRef, effect, OnInit, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  TripDayPlan,
  TripParkCandidate,
  TripParkCandidateState,
  TripPlan,
  TripProgram
} from '@app/models/trips/trip.models';
import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import { TranslationService } from '@app/services/translation.service';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { TripCandidateCardComponent } from '../../components/trip-candidate-card/trip-candidate-card.component';
import { areTripDateInputsValid } from '../../state/trip-date-proposal.helpers';
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
  protected readonly currentLanguage = signal<string>('en');
  protected readonly startDate = signal<string>('');
  protected readonly endDate = signal<string>('');
  protected readonly destinationTimeZoneId = signal<string>('');
  protected readonly dateEditorEnabled = signal<boolean>(true);
  protected readonly selectedWishlistIds = signal<Set<string>>(new Set<string>());
  protected readonly dayDrafts = signal<Record<string, TripDayDraft>>({});
  protected readonly imageWidths: readonly number[] = [120, 200, 320];

  private dateDraftInitialized: boolean = false;
  private handledDateDraftRevision: number = 0;
  private handledDayRecoveryRevision: number = 0;
  private handledClearDayRevision: number = 0;

  constructor(
    protected readonly facade: TripOverviewStateFacade,
    private readonly route: ActivatedRoute,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      route,
      translationService.getCurrentLang() || 'en'
    );
    this.currentLanguage.set(initialLanguage);
    findNearestLanguageActivatedRoute(route)?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(destroyRef)
    ).subscribe((params: ParamMap): void => {
      this.currentLanguage.set(resolveLanguageFromParamMap(params, this.currentLanguage()));
    });

    effect((): void => {
      const trip: TripPlan | null = this.facade.trip();
      const dateDraftRevision: number = this.facade.dateDraftRevision();
      const mustRefresh: boolean = dateDraftRevision > this.handledDateDraftRevision;
      if (!trip || (this.dateDraftInitialized && !mustRefresh)) {
        return;
      }
      this.startDate.set(trip.dateProposal.startDate ?? '');
      this.endDate.set(trip.dateProposal.endDate ?? '');
      this.destinationTimeZoneId.set(trip.destinationTimeZoneId ?? '');
      this.dateEditorEnabled.set(trip.dateProposal.kind !== 'Candidates' && trip.dateProposal.kind !== 'Range');
      this.dateDraftInitialized = true;
      this.handledDateDraftRevision = dateDraftRevision;
    });

    effect((): void => {
      const dates: string[] = this.facade.tripDates();
      const program: TripProgram = this.facade.program();
      const days: TripDayPlan[] = program.days;
      const recoveryRevision: number = this.facade.recoveryRevision();
      const clearedDay: { localDate: string; revision: number } | null = this.facade.clearedDay();
      const mustRecover: boolean = recoveryRevision > this.handledDayRecoveryRevision;
      const hasNewClearedDay: boolean = (clearedDay?.revision ?? 0) > this.handledClearDayRevision;
      const existingDrafts: Record<string, TripDayDraft> = untracked(this.dayDrafts);
      const nextDrafts: Record<string, TripDayDraft> = {};
      for (const date of dates) {
        const saved: TripDayPlan | undefined = days.find((day: TripDayPlan): boolean => day.localDate === date);
        const mustRefreshDate: boolean = mustRecover || (hasNewClearedDay && clearedDay?.localDate === date);
        const existingDraft: TripDayDraft | undefined = existingDrafts[date];
        const existingCandidateIsValid: boolean = !existingDraft?.candidateId
          || saved?.parkCandidateId === existingDraft.candidateId
          || program.candidates.some((candidate: TripParkCandidate): boolean =>
            candidate.candidateId === existingDraft.candidateId
            && candidate.state === 'Selected'
            && candidate.isParkAvailable
            && (candidate.candidateDates.length === 0 || candidate.candidateDates.includes(date)));
        nextDrafts[date] = !mustRefreshDate && existingDraft ? {
          ...existingDraft,
          candidateId: existingCandidateIsValid ? existingDraft.candidateId : ''
        } : {
          candidateId: saved?.parkCandidateId ?? '',
          arrivalTime: saved?.desiredArrivalTime ?? '',
          note: saved?.groupNote ?? ''
        };
      }
      this.dayDrafts.set(nextDrafts);
      this.handledDayRecoveryRevision = recoveryRevision;
      this.handledClearDayRevision = clearedDay?.revision ?? this.handledClearDayRevision;
    });
  }

  ngOnInit(): void {
    const tripId: string = this.route.snapshot.paramMap.get('tripId') ?? '';
    this.facade.load(tripId);
  }

  protected saveDates(): void {
    if (this.canSaveDates()) {
      this.facade.setDates(this.startDate(), this.endDate(), this.destinationTimeZoneId());
    }
  }

  protected clearDates(): void {
    if (this.canClearDates()) {
      this.facade.setDates('', '', '');
    }
  }

  protected beginFixedDateEdit(): void {
    this.dateEditorEnabled.set(true);
  }

  protected canSaveDates(): boolean {
    const hasStartDate: boolean = !!this.startDate().trim();
    return this.dateEditorEnabled()
      && hasStartDate
      && areTripDateInputsValid(this.startDate(), this.endDate())
      && !!this.destinationTimeZoneId().trim()
      && !this.dateDependenciesOutsideDraft();
  }

  protected datesInvalid(): boolean {
    return !areTripDateInputsValid(this.startDate(), this.endDate());
  }

  protected hasSpecialDateProposal(): boolean {
    const kind: TripPlan['dateProposal']['kind'] | undefined = this.facade.trip()?.dateProposal.kind;
    return kind === 'Candidates' || kind === 'Range';
  }

  protected canClearDates(): boolean {
    const program: TripProgram = this.facade.program();
    return program.days.length === 0
      && program.candidates.every((candidate: TripParkCandidate): boolean => candidate.candidateDates.length === 0);
  }

  protected dateDependenciesOutsideDraft(): boolean {
    const normalizedStart: string = this.startDate().trim();
    const normalizedEnd: string = this.endDate().trim() || normalizedStart;
    if (!normalizedStart || !areTripDateInputsValid(normalizedStart, normalizedEnd)) {
      return false;
    }

    const program: TripProgram = this.facade.program();
    return program.days.some((day: TripDayPlan): boolean =>
      day.localDate < normalizedStart || day.localDate > normalizedEnd)
      || program.candidates.some((candidate: TripParkCandidate): boolean =>
        candidate.candidateDates.some((date: string): boolean =>
          date < normalizedStart || date > normalizedEnd));
  }

  protected currentProposedDates(): string[] {
    const trip: TripPlan | null = this.facade.trip();
    if (!trip) {
      return [];
    }
    if (trip.dateProposal.kind === 'Candidates') {
      return trip.dateProposal.candidateDates;
    }
    return [trip.dateProposal.startDate, trip.dateProposal.endDate].filter(
      (date: string | null): date is string => !!date
    );
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

  protected isCandidateScheduled(candidateId: string): boolean {
    return this.facade.program().days.some(
      (day: TripDayPlan): boolean => day.parkCandidateId === candidateId
    );
  }

  protected selectedCandidates(): TripParkCandidate[] {
    return this.facade.program().candidates.filter(
      (candidate: TripParkCandidate): boolean => candidate.state === 'Selected' && candidate.isParkAvailable
    );
  }

  protected selectedCandidatesForDate(localDate: string): TripParkCandidate[] {
    return this.selectedCandidates().filter((candidate: TripParkCandidate): boolean =>
      candidate.candidateDates.length === 0 || candidate.candidateDates.includes(localDate));
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

  protected canSaveDay(date: string): boolean {
    const candidateId: string | undefined = this.dayDrafts()[date]?.candidateId;
    return !!candidateId && this.selectedCandidatesForDate(date).some(
      (candidate: TripParkCandidate): boolean => candidate.candidateId === candidateId
    );
  }

  protected clearDay(date: string): void {
    this.facade.clearDay(date);
  }

  protected hasSavedDay(date: string): boolean {
    return this.savedDay(date) !== undefined;
  }

  protected savedDay(date: string): TripDayPlan | undefined {
    return this.facade.program().days.find((day: TripDayPlan): boolean => day.localDate === date);
  }

  protected savedDayNeedsFallbackOption(date: string): boolean {
    const saved: TripDayPlan | undefined = this.savedDay(date);
    return !!saved && !this.selectedCandidatesForDate(date).some(
      (candidate: TripParkCandidate): boolean => candidate.candidateId === saved.parkCandidateId
    );
  }

  protected stateLabelKey(state: TripParkCandidateState): string {
    return `trips.candidates.states.${state.toLowerCase()}`;
  }
}
