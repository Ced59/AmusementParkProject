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
});
