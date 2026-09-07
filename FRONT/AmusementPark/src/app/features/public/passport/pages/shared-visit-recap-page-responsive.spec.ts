import { SharedVisitRecapPageComponent } from './shared-visit-recap-page.component';

describe('SharedVisitRecapPageComponent responsive contract', () => {
  it('keeps the public story within 320px and collapses cards without horizontal overflow', () => {
    const styles: string = (
      SharedVisitRecapPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: calc(100% - 2rem)');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow: hidden');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('@media (max-width: 390px)');
    expect(styles).toContain('white-space: normal');
  });
});
