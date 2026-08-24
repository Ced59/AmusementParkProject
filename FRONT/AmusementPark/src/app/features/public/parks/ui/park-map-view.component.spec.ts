import { ParkMapViewComponent } from './park-map-view.component';

describe('ParkMapViewComponent', () => {
  it('keeps long park names inside the mobile hero', () => {
    const styles: string = (
      ParkMapViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('.park-subpage-hero__content');
    expect(styles).toMatch(/\.park-subpage-hero__meta[\s\S]*app-ui-chip/);
    expect(styles).toContain(':is(a');
    expect(styles).toContain('button');
    expect(styles).toContain('span');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('white-space: normal');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});
