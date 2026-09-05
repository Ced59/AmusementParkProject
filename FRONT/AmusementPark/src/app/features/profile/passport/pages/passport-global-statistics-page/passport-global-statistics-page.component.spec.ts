import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PASSPORT_GLOBAL_STATISTICS_FILTER_STORE } from '../../state/passport-global-statistics-filter.ports';
import { PASSPORT_STATISTICS_API_PORT } from '../../state/passport-statistics-state-data.ports';
import { PassportGlobalStatisticsPageComponent } from './passport-global-statistics-page.component';

describe('PassportGlobalStatisticsPageComponent', () => {
  it('keeps charts bounded on mobile and exposes filters without technical identifiers', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PassportGlobalStatisticsPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: PASSPORT_GLOBAL_STATISTICS_FILTER_STORE, useValue: { read: () => ({ year: null, parkId: null }), write: vi.fn() } },
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: { getGlobalStatistics: () => of(createStatistics()) } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PassportGlobalStatisticsPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Parc test');
    expect(host.textContent).not.toContain('park-technical-id');
    expect(host.querySelectorAll('table').length).toBeGreaterThan(0);
    const styles: string = (PassportGlobalStatisticsPageComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('grid-template-columns: 1fr');
  });
});

function createStatistics() {
  return {
    selectedYear: null, selectedParkId: null, availableYears: [2025],
    availableParks: [{ parkId: 'park-technical-id', parkName: 'Parc test' }], parkCount: 1,
    summary: {
      visitCount: 1, approximateVisitCount: 0,
      parkRatingCoverage: { ratedCount: 0, totalCount: 1, rate: 0 }, historicalParkRatings: null,
      firstVisit: null, lastVisit: null,
      rideOutcomes: { recordedOutcomeCount: 1, completedRideCount: 1, attemptedCount: 0, missedClosedCount: 0, missedUnavailableCount: 0, skippedByChoiceCount: 0 },
      rideRatingCoverage: { ratedCount: 0, totalCount: 1, rate: 0 }, historicalRideRatings: null,
      distinctCompletedItemCount: 1, repeatedCompletedItemCount: 0, categoryCoverage: []
    },
    activityByYear: [{ year: 2025, visitCount: 1, recordedRideCount: 1 }],
    topParks: [{ parkId: 'park-technical-id', parkName: 'Parc test', visitCount: 1, recordedRideCount: 1 }],
    topItems: [], ratingEvolution: []
  };
}
