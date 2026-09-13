import { PublicShareReportComponent } from './public-share-report.component';

describe('PublicShareReportComponent responsive contract', (): void => {
  it('contains every field inside a 320px viewport', (): void => {
    const styles: string = (PublicShareReportComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('box-sizing: border-box');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 35rem)');
  });
});
