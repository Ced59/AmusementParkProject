import { ParkFitComparisonPageComponent } from './park-fit-comparison-page.component';

describe('Park Fit comparison responsive contract', () => {
  it('turns wide comparison rows into contained mobile cards', () => {
    const styles: string = (
      ParkFitComparisonPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('repeat(var(--park-count), minmax(0, 1fr))');
    expect(styles).toContain('@media (max-width: 960px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('.park-fit-comparison__cell-park');
    expect(styles).not.toContain('.park-fit-comparison__cell-park {\n  display: none');
  });
});
