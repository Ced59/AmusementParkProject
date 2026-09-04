import { PassportStatisticsPageComponent } from './passport-statistics-page.component';

describe('PassportStatisticsPageComponent responsive contract', () => {
  const styles: string = (
    PassportStatisticsPageComponent as unknown as { ɵcmp: { styles: string[] } }
  ).ɵcmp.styles.join('\n');

  it('constrains every page branch to the viewport', () => {
    expect(styles).toContain('width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('overflow-wrap: anywhere');
  });

  it('reflows cards and headings while preserving the fixed mobile navigation safe area', () => {
    expect(styles).toContain('@media (max-width: 900px)');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('padding-bottom: calc(5.75rem + env(safe-area-inset-bottom))');
    expect(styles).toContain('@media (max-width: 360px)');
  });
});
