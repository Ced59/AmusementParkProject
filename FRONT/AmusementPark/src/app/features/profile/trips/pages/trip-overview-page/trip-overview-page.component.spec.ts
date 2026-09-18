import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { TripDayPlan, TripParkCandidate, TripPlan, TripProgram } from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { TripOverviewStateFacade } from '../../state/trip-overview-state.facade';
import { TripOverviewPageComponent } from './trip-overview-page.component';

describe('TripOverviewPageComponent', () => {
  it('replaces stale date and day drafts with the server state after conflict recovery', async () => {
    const languageParams: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(
      convertToParamMap({ lang: 'fr' })
    );
    const trip: WritableSignal<TripPlan | null> = signal<TripPlan | null>(createTrip());
    const program: WritableSignal<TripProgram> = signal<TripProgram>(createProgram('09:00', 'Serveur initial'));
    const tripDates: WritableSignal<string[]> = signal<string[]>(['2026-10-03']);
    const recoveryRevision: WritableSignal<number> = signal<number>(0);
    const dateDraftRevision: WritableSignal<number> = signal<number>(0);
    const clearedDay: WritableSignal<{ localDate: string; revision: number } | null> = signal(null);
    const busy: WritableSignal<boolean> = signal<boolean>(false);
    const wishlistLoading: WritableSignal<boolean> = signal<boolean>(false);
    const facade = {
      trip: trip.asReadonly(),
      program: program.asReadonly(),
      tripDates: tripDates.asReadonly(),
      recoveryRevision: recoveryRevision.asReadonly(),
      dateDraftRevision: dateDraftRevision.asReadonly(),
      clearedDay: clearedDay.asReadonly(),
      status: signal('ready').asReadonly(),
      busy: busy.asReadonly(),
      actionError: signal(null).asReadonly(),
      wishlistParks: signal([]).asReadonly(),
      wishlistLoading: wishlistLoading.asReadonly(),
      wishlistUnavailable: signal(false).asReadonly(),
      load: vi.fn(),
      setDates: vi.fn(),
      setSpecialProposalTimeZone: vi.fn(),
      importWishlist: vi.fn(),
      changeCandidateState: vi.fn(),
      moveCandidate: vi.fn(),
      saveDay: vi.fn(),
      clearDay: vi.fn(),
      imageIdForPark: vi.fn().mockReturnValue(null)
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, TripOverviewPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ tripId: 'trip-1' }) },
            parent: {
              snapshot: { paramMap: languageParams.value },
              paramMap: languageParams.asObservable(),
              parent: null
            }
          }
        },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } }
      ]
    })
      .overrideComponent(TripOverviewPageComponent, {
        set: { providers: [{ provide: TripOverviewStateFacade, useValue: facade }] }
      })
      .compileComponents();

    const fixture: ComponentFixture<TripOverviewPageComponent> = TestBed.createComponent(TripOverviewPageComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('trips.actions.clearDay');
    expect(fixture.nativeElement.querySelector('textarea')?.maxLength).toBe(2000);
    wishlistLoading.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('trips.wishlist.loading');
    wishlistLoading.set(false);
    fixture.detectChanges();
    const state = fixture.componentInstance as unknown as {
      startDate: WritableSignal<string>;
      endDate: WritableSignal<string>;
      destinationTimeZoneId: WritableSignal<string>;
      dateEditorEnabled: WritableSignal<boolean>;
      currentLanguage: () => string;
      dayDrafts: WritableSignal<Record<string, { candidateId: string; arrivalTime: string; note: string }>>;
      selectedCandidatesForDate: (localDate: string) => TripParkCandidate[];
      canSaveDay: (localDate: string) => boolean;
      canSaveDates: () => boolean;
      datesTooLong: () => boolean;
      canSaveSpecialTimeZone: () => boolean;
      canClearDates: () => boolean;
      hasUnsavedDayDraft: () => boolean;
      dateDependenciesOutsideDraft: () => boolean;
      unsavedDayDraftOutsideRange: () => boolean;
      isCandidateScheduled: (candidateId: string) => boolean;
      saveDates: () => void;
      saveSpecialTimeZone: () => void;
      clearDates: () => void;
    };
    state.startDate.set('2026-10-03');
    state.endDate.set('2026-10-04');
    state.dayDrafts.set({
      '2026-10-03': { candidateId: 'candidate-1', arrivalTime: '08:00', note: 'Brouillon obsolète' }
    });
    expect(state.currentLanguage()).toBe('fr');
    languageParams.next(convertToParamMap({ lang: 'de' }));
    fixture.detectChanges();
    expect(state.currentLanguage()).toBe('de');
    expect(fixture.nativeElement.querySelector('a')?.getAttribute('href')).toContain('/de/profile/trips');
    busy.set(true);
    fixture.detectChanges();
    expect(Array.from(fixture.nativeElement.querySelectorAll('.trip-dates input')).every(
      (input: unknown): boolean => (input as HTMLInputElement).disabled
    )).toBe(true);
    expect(Array.from(fixture.nativeElement.querySelectorAll('.day-card select, .day-card input, .day-card textarea, .day-card button')).every(
      (control: unknown): boolean => (control as HTMLInputElement).disabled
    )).toBe(true);
    busy.set(false);
    fixture.detectChanges();

    trip.set(createTrip({
      version: 2,
      destinationTimeZoneId: 'Europe/Berlin',
      dateProposal: { kind: 'Fixed', startDate: '2026-10-04', endDate: '2026-10-04', candidateDates: [] }
    }));
    tripDates.set(['2026-10-04']);
    program.set(createProgram('10:30', 'Version concurrente', '2026-10-04'));
    recoveryRevision.set(1);
    dateDraftRevision.set(1);
    fixture.detectChanges();

    expect(state.startDate()).toBe('2026-10-04');
    expect(state.endDate()).toBe('2026-10-04');
    expect(state.destinationTimeZoneId()).toBe('Europe/Berlin');
    expect(state.dayDrafts()['2026-10-04']).toEqual({
      candidateId: 'candidate-1',
      arrivalTime: '10:30',
      note: 'Version concurrente'
    });

    state.endDate.set('2026-10-04');
    trip.set(createTrip({
      version: 3,
      destinationTimeZoneId: null,
      dateProposal: { kind: 'None', startDate: null, endDate: null, candidateDates: [] }
    }));
    dateDraftRevision.set(2);
    fixture.detectChanges();

    expect(state.startDate()).toBe('');
    expect(state.endDate()).toBe('');
    expect(state.destinationTimeZoneId()).toBe('');

    tripDates.set(['2026-10-04', '2026-10-05']);
    state.dayDrafts.set({
      '2026-10-04': { candidateId: 'candidate-1', arrivalTime: '10:30', note: 'À effacer' },
      '2026-10-05': { candidateId: 'candidate-2', arrivalTime: '08:45', note: 'Brouillon à conserver' }
    });
    program.set({ candidates: [createCandidate('candidate-2', [])], days: [] });
    clearedDay.set({ localDate: '2026-10-04', revision: 1 });
    fixture.detectChanges();

    expect(state.dayDrafts()['2026-10-04']).toEqual({ candidateId: '', arrivalTime: '', note: '' });
    expect(state.dayDrafts()['2026-10-05']).toEqual({
      candidateId: 'candidate-2', arrivalTime: '08:45', note: 'Brouillon à conserver'
    });

    program.set({
      ...createProgram('10:30', 'Version concurrente', '2026-10-04'),
      candidates: [
        createCandidate('candidate-restricted', ['2026-10-03']),
        createCandidate('candidate-flexible', [])
      ]
    });
    fixture.detectChanges();

    expect(state.selectedCandidatesForDate('2026-10-04').map((candidate: TripParkCandidate): string =>
      candidate.candidateId)).toEqual(['candidate-flexible']);
    expect(state.isCandidateScheduled('candidate-1')).toBe(true);
    expect(state.isCandidateScheduled('candidate-flexible')).toBe(false);

    tripDates.set(['2026-10-05']);
    state.dayDrafts.set({
      '2026-10-05': { candidateId: 'candidate-flexible', arrivalTime: '09:00', note: 'À préserver' }
    });
    program.set({ candidates: [createCandidate('candidate-flexible', [], 'Proposed')], days: [] });
    fixture.detectChanges();

    expect(state.dayDrafts()['2026-10-05']).toEqual({
      candidateId: '', arrivalTime: '09:00', note: 'À préserver'
    });
    expect(state.canSaveDay('2026-10-05')).toBe(false);

    trip.set(createTrip({
      version: 4,
      dateProposal: {
        kind: 'Candidates', startDate: null, endDate: null,
        candidateDates: ['2026-11-01', '2026-11-08']
      }
    }));
    dateDraftRevision.set(3);
    fixture.detectChanges();

    expect(state.dateEditorEnabled()).toBe(false);
    expect(state.canSaveDates()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('2026-11-01');
    expect(fixture.nativeElement.textContent).toContain('trips.actions.saveTimeZone');
    state.destinationTimeZoneId.set('Europe/Berlin');
    expect(state.canSaveSpecialTimeZone()).toBe(true);
    state.saveSpecialTimeZone();
    expect(facade.setSpecialProposalTimeZone).toHaveBeenCalledWith('Europe/Berlin');
    state.dateEditorEnabled.set(true);
    expect(state.startDate()).toBe('');
    expect(state.canSaveDates()).toBe(false);
    state.dayDrafts.set({});
    program.set({ candidates: [createCandidate('candidate-restricted', ['2026-11-01'])], days: [] });
    fixture.detectChanges();
    expect(state.canClearDates()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('trips.dates.clearBlocked');
    state.startDate.set('2026-11-02');
    state.endDate.set('2026-11-08');
    state.destinationTimeZoneId.set('Europe/Paris');
    expect(state.dateDependenciesOutsideDraft()).toBe(true);
    expect(state.canSaveDates()).toBe(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('trips.dates.dependenciesOutsideRange');
    state.startDate.set('2026-11-01');
    expect(state.dateDependenciesOutsideDraft()).toBe(false);
    expect(state.canSaveDates()).toBe(true);
    state.dayDrafts.set({
      '2026-10-31': { candidateId: '', arrivalTime: '09:15', note: 'Brouillon non enregistré' },
      '2026-11-02': { candidateId: '', arrivalTime: '', note: '' }
    });
    fixture.detectChanges();
    expect(state.unsavedDayDraftOutsideRange()).toBe(true);
    expect(state.dateDependenciesOutsideDraft()).toBe(true);
    expect(state.canSaveDates()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('trips.dates.unsavedDayOutsideRange');
    state.dayDrafts.set({});
    state.startDate.set('2026-01-01');
    state.endDate.set('2027-01-02');
    fixture.detectChanges();
    expect(state.datesTooLong()).toBe(true);
    expect(state.canSaveDates()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('trips.feedback.dateRangeTooLong');
    state.dateEditorEnabled.set(false);
    state.saveDates();
    expect(facade.setDates).not.toHaveBeenCalled();
    state.clearDates();
    expect(facade.setDates).not.toHaveBeenCalled();

    program.set({ candidates: [createCandidate('candidate-flexible', [])], days: [] });
    fixture.detectChanges();
    expect(state.canClearDates()).toBe(true);
    state.dayDrafts.set({
      '2026-11-01': { candidateId: 'candidate-flexible', arrivalTime: '', note: 'À préserver' }
    });
    fixture.detectChanges();
    expect(state.hasUnsavedDayDraft()).toBe(true);
    expect(state.canClearDates()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('trips.dates.unsavedDayBeforeClear');
    state.clearDates();
    expect(facade.setDates).not.toHaveBeenCalled();
    state.dayDrafts.set({});
    fixture.detectChanges();
    expect(state.canClearDates()).toBe(true);
    state.clearDates();
    expect(facade.setDates).toHaveBeenCalledWith('', '', '');
  });
});

function createTrip(overrides: Partial<TripPlan> = {}): TripPlan {
  return {
    tripPlanId: 'trip-1', title: 'Voyage',
    dateProposal: { kind: 'Fixed', startDate: '2026-10-03', endDate: '2026-10-04', candidateDates: [] },
    destinationTimeZoneId: 'Europe/Paris', status: 'Draft', accessScope: 'Private', memberCount: 1,
    isOwner: true, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z', version: 1,
    ...overrides
  };
}

function createProgram(arrivalTime: string, note: string, localDate: string = '2026-10-03'): TripProgram {
  const day: TripDayPlan = {
    dayPlanId: 'day-1', localDate, parkCandidateId: 'candidate-1', parkId: 'park-1', parkName: 'Parc A',
    isParkAvailable: true, desiredArrivalTime: arrivalTime, groupNote: note, blocks: [], version: 1,
    createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z'
  };
  return { candidates: [], days: [day] };
}

function createCandidate(
  candidateId: string,
  candidateDates: string[],
  state: TripParkCandidate['state'] = 'Selected'
): TripParkCandidate {
  return {
    candidateId, parkId: `park-${candidateId}`, parkName: candidateId, isParkAvailable: true,
    candidateDates, source: 'Manual', state, collectiveNote: null, fitSnapshot: null,
    sortPosition: 0, version: 1, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z'
  };
}
