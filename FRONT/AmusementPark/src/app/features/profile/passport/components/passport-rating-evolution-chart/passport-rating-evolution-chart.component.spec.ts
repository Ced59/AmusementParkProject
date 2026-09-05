import { TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PassportRatingEvolutionChartComponent } from './passport-rating-evolution-chart.component';

describe('PassportRatingEvolutionChartComponent', () => {
  it('keeps its graph bounded and its equivalent table locally scrollable', () => {
    const styles: string = (PassportRatingEvolutionChartComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: auto');
    expect(styles).toContain('@media (max-width: 480px)');
    expect(styles).toContain('@media (prefers-reduced-motion: reduce)');
  });

  it('does not draw a colored bar when a yearly average is absent', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PassportRatingEvolutionChartComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
    const fixture = TestBed.createComponent(PassportRatingEvolutionChartComponent);
    fixture.componentRef.setInput('points', [{
      year: 2025,
      parkAverage: null,
      ratedVisitCount: 0,
      rideAverage: 4,
      ratedRideCount: 2
    }]);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.rating-chart__bar--park')).toBeNull();
    expect(host.querySelector('.rating-chart__bar--ride')).not.toBeNull();
  });
});
