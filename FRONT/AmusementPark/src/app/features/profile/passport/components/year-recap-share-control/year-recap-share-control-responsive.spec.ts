import { YearRecapShareControlComponent } from './year-recap-share-control.component';

describe('YearRecapShareControlComponent responsive contract', () => {
  it('bounds the editor and stacks dense actions on narrow mobile viewports', () => {
    const styles: string = (
      YearRecapShareControlComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('white-space: normal');
  });
});
