import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PassportHistoricalStatistics } from '@app/models/passport/passport-statistics.models';
import { PASSPORT_STATISTICS_API_PORT } from '../../state/passport-statistics-state-data.ports';
import { PassportHistoricalStatisticsPageComponent } from './passport-historical-statistics-page.component';

describe('PassportHistoricalStatisticsPageComponent', () => {
  it('renders named evidence without technical identifiers and stays bounded on mobile', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PassportHistoricalStatisticsPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: PASSPORT_STATISTICS_API_PORT, useValue: { getHistoricalStatistics: () => of(createStatistics()) } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PassportHistoricalStatisticsPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Parc témoin');
    expect(host.textContent).toContain('Ancien Cyclone');
    expect(host.textContent).toContain('parkExplorer.types.darkRide');
    expect(host.textContent).toContain('parkExplorer.types.rollerCoaster');
    expect(host.textContent).not.toContain('park-technical-id');
    expect(host.querySelectorAll('app-passport-global-bar-chart')).toHaveLength(2);
    const styles: string = (PassportHistoricalStatisticsPageComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
  });
});

function createStatistics(): PassportHistoricalStatistics {
  return {
    visitCount: 2,
    firstVisitYear: 2001,
    completedRideCount: 2,
    canonicallyResolvedRideCount: 2,
    canonicalCoverageRate: 1,
    parkCountAcrossMultipleEras: 1,
    parksAcrossEras: [{ parkName: 'Parc témoin', firstVisitYear: 2001, lastVisitYear: 2026, visitCount: 2, canonicalEraCount: 2 }],
    disappearedAttractions: [{ parkName: 'Parc témoin', attractionName: 'Ancien Cyclone', firstVisitYear: 2001, lastVisitYear: 2001, completedRideCount: 1 }],
    transformations: [{ parkName: 'Parc témoin', currentName: 'Nouveau Cyclone', currentCategory: 'RollerCoaster', namesAtVisit: ['Nouveau Cyclone'], categoriesAtVisit: ['DarkRide'], firstVisitYear: 2001, lastVisitYear: 2001, completedRideCount: 1 }],
    historicalNames: [{ parkName: 'Parc témoin', nameAtVisit: 'Ancien Cyclone', currentName: 'Nouveau Cyclone', firstVisitYear: 2001, lastVisitYear: 2001, completedRideCount: 1 }],
    historicalCategories: [{ category: 'Attraction', completedRideCount: 2, distinctAttractionCount: 1 }]
  };
}
