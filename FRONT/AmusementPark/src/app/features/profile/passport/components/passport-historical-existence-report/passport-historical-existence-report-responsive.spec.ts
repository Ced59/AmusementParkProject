import { PassportHistoricalExistenceReportComponent } from './passport-historical-existence-report.component';

describe('Passport historical existence report responsive contract', () => {
  it('keeps long evidence and controls inside a narrow mobile viewport', () => {
    const styles: string = (
      PassportHistoricalExistenceReportComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('box-sizing: border-box');
    expect(styles).toContain('@media (max-width: 640px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('width: 100%');
  });
});
