import { SharedYearRecapPageComponent } from './shared-year-recap-page.component';

describe('SharedYearRecapPageComponent responsive contract', () => {
  it('keeps the public story within the viewport down to 320px', () => {
    const styles: string = (
      SharedYearRecapPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('width: 76rem');
    expect(styles).toContain('max-width: calc(100% - 2rem)');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('@media (max-width: 360px)');
  });
});
