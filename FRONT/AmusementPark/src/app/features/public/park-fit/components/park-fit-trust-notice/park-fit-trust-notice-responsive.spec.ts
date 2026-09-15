import { ParkFitTrustNoticeComponent } from './park-fit-trust-notice.component';

describe('Park Fit trust notice responsive contract', () => {
  it('contains every assurance inside a 360 pixel viewport', () => {
    const styles: string = (
      ParkFitTrustNoticeComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('grid-template-columns: auto minmax(0, 1fr)');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
