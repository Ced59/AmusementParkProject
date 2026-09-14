import { AdminParkFitDataQualityComponent } from './admin-park-fit-data-quality.component';

describe('admin park fit data quality responsive contract', () => {
  it('contains long content and reflows cards down to narrow mobile viewports', () => {
    const styles: string = (
      AdminParkFitDataQualityComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 480px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('@media (max-height: 520px) and (orientation: landscape)');
  });
});
