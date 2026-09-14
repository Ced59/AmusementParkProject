import { ParkFitSourceReportComponent } from './park-fit-source-report.component';

describe('Park Fit source report responsive contract', () => {
  it('contains the report form inside a 360 pixel viewport', () => {
    const styles: string = (
      ParkFitSourceReportComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('box-sizing: border-box');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 420px)');
  });
});
