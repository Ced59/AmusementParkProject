import { ParkFitStartPageComponent } from './park-fit-start-page.component';

describe('Park Fit start page responsive contract', () => {
  it('contains the form and result cards inside narrow viewports', () => {
    const styles: string = (
      ParkFitStartPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('minmax(min(100%, 11rem), 1fr)');
    expect(styles).toContain('@media (max-width: 560px)');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('.park-fit-location');
    expect(styles).toContain('> button');
    expect(styles).toContain('grid-column: 1/-1');
  });
});
