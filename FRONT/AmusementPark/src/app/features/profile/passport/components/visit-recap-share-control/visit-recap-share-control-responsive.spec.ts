import { VisitRecapShareControlComponent } from './visit-recap-share-control.component';

describe('VisitRecapShareControlComponent responsive contract', () => {
  it('bounds the complete workshop and stacks dense controls on narrow mobile viewports', () => {
    const styles: string = (
      VisitRecapShareControlComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow: hidden');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 680px)');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('white-space: normal');
  });
});
