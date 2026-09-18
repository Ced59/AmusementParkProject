import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { TripDayPlan, TripParkCandidate, TripPlan, TripProgram } from '@app/models/trips/trip.models';
import { TranslationService } from '@app/services/translation.service';
import { TripOverviewStateFacade } from '../../state/trip-overview-state.facade';
import { TripOverviewPageComponent } from './trip-overview-page.component';

describe('TripOverviewPageComponent', () => {
  it('replaces stale date and day drafts with the server state after conflict recovery', async () => {
    const trip: WritableSignal<TripPlan | null> = signal<TripPlan | null>(createTrip());
    const program: WritableSignal<TripProgram> = signal<TripProgram>(createProgram('09:00', 'Serveur initial'));
    const tripDates: WritableSignal<string[]> = signal<string[]>(['2026-10-03']);
    const recoveryRevision: WritableSignal<number> = signal<number>(0);
    const dayDraftRevision: WritableSignal<number> = signal<number>(0);
    const facade = {
      trip: trip.asReadonly(),
      program: program.asReadonly(),
      tripDates: tripDates.asReadonly(),
      recoveryRevision: recoveryRevision.asReadonly(),
      dayDraftRevision: dayDraftRevision.asReadonly(),
      status: signal('ready').asReadonly(),
      busy: signal(false).asReadonly(),
      actionError: signal(null).asReadonly(),
      wishlistParks: signal([]).asReadonly(),
      load: vi.fn(),
      setDates: vi.fn(),
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
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (): string => 'trip-1' } } } },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr' } }
      ]
    })
      .overrideComponent(TripOverviewPageComponent, {
        set: { providers: [{ provide: TripOverviewStateFacade, useValue: facade }] }
      })
      .compileComponents();

    const fixture: ComponentFixture<TripOverviewPageComponent> = TestBed.createComponent(TripOverviewPageComponent);
    fixture.detectChanges();
    const state = fixture.componentInstance as unknown as {
      startDate: WritableSignal<string>;
      endDate: WritableSignal<string>;
      destinationTimeZoneId: WritableSignal<string>;
      dayDrafts: WritableSignal<Record<string, { candidateId: string; arrivalTime: string; note: string }>>;
      selectedCandidatesForDate: (localDate: string) => TripParkCandidate[];
    };
    state.startDate.set('2026-10-03');
    state.endDate.set('2026-10-04');
    state.dayDrafts.set({
      '2026-10-03': { candidateId: 'candidate-1', arrivalTime: '08:00', note: 'Brouillon obsolète' }
    });

    trip.set(createTrip({
      version: 2,
      destinationTimeZoneId: 'Europe/Berlin',
      dateProposal: { kind: 'Fixed', startDate: '2026-10-04', endDate: '2026-10-04', candidateDates: [] }
    }));
    tripDates.set(['2026-10-04']);
    program.set(createProgram('10:30', 'Version concurrente', '2026-10-04'));
    recoveryRevision.set(1);
    dayDraftRevision.set(1);
    fixture.detectChanges();

    expect(state.startDate()).toBe('2026-10-04');
    expect(state.endDate()).toBe('2026-10-04');
    expect(state.destinationTimeZoneId()).toBe('Europe/Berlin');
    expect(state.dayDrafts()['2026-10-04']).toEqual({
      candidateId: 'candidate-1',
      arrivalTime: '10:30',
      note: 'Version concurrente'
    });

    program.set({ candidates: [], days: [] });
    dayDraftRevision.set(2);
    fixture.detectChanges();

    expect(state.dayDrafts()['2026-10-04']).toEqual({ candidateId: '', arrivalTime: '', note: '' });

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

function createCandidate(candidateId: string, candidateDates: string[]): TripParkCandidate {
  return {
    candidateId, parkId: `park-${candidateId}`, parkName: candidateId, isParkAvailable: true,
    candidateDates, source: 'Manual', state: 'Selected', collectiveNote: null, fitSnapshot: null,
    sortPosition: 0, version: 1, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z'
  };
}
