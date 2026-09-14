import { ParkFitGroupProfilesPageComponent } from './park-fit-group-profiles-page.component';

describe('Park Fit group profiles responsive contract', () => {
  it('contains every card and control inside a 360px viewport', () => {
    const styles: string = (
      ParkFitGroupProfilesPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('minmax(min(100%, 17rem), 1fr)');
    expect(styles).toContain('min-height: 44px');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
