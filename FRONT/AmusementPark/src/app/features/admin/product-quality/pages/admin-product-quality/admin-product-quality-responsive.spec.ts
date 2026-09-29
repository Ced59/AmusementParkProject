import { AdminProductQualityComponent } from './admin-product-quality.component';

describe('AdminProductQualityComponent responsive contract', (): void => {
  it('contains long content and collapses every grid on small viewports', (): void => {
    const styles: string = (
      AdminProductQualityComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 880px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('@media (max-width: 360px)');
    expect(styles).toContain('@media (max-height: 520px) and (orientation: landscape)');
  });
});
