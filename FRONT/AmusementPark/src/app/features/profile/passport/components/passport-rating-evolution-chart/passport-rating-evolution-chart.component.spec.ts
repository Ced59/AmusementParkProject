import { PassportRatingEvolutionChartComponent } from './passport-rating-evolution-chart.component';

describe('PassportRatingEvolutionChartComponent', () => {
  it('keeps its graph bounded and its equivalent table locally scrollable', () => {
    const styles: string = (PassportRatingEvolutionChartComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: auto');
    expect(styles).toContain('@media (max-width: 480px)');
    expect(styles).toContain('@media (prefers-reduced-motion: reduce)');
  });
});
