import { TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PassportGlobalBarChartComponent } from './passport-global-bar-chart.component';

describe('PassportGlobalBarChartComponent', () => {
  it('bounds visual rows, wraps labels and keeps an accessible table on narrow screens', () => {
    const styles: string = (PassportGlobalBarChartComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('overflow-x: auto');
    expect(styles).toContain('@media (max-width: 560px)');
    expect(styles).toContain('@media (prefers-reduced-motion: reduce)');
  });

  it('does not draw a colored bar for a zero-valued series', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PassportGlobalBarChartComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
    const fixture = TestBed.createComponent(PassportGlobalBarChartComponent);
    fixture.componentRef.setInput('titleKey', 'chart.title');
    fixture.componentRef.setInput('descriptionKey', 'chart.description');
    fixture.componentRef.setInput('primaryLegendKey', 'chart.primary');
    fixture.componentRef.setInput('secondaryLegendKey', 'chart.secondary');
    fixture.componentRef.setInput('rows', [{
      id: 'row-1',
      label: 'Ligne',
      primaryValue: 0,
      secondaryValue: 2
    }]);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.passport-chart__bar--primary')).toBeNull();
    expect(host.querySelector('.passport-chart__bar--secondary')).not.toBeNull();
  });
});
