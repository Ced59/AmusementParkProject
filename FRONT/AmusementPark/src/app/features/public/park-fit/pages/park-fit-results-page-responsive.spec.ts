import { ParkFitResultsPageComponent } from './park-fit-results-page.component';

describe('Park Fit results page responsive contract', () => {
  it('contains all explanatory cards inside narrow viewports', () => {
    const styles: string = (
      ParkFitResultsPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('minmax(min(100%, 12rem), 1fr)');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
