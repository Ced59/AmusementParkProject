import { AdminPassportBetaComponent } from './admin-passport-beta.component';

describe('admin passport beta responsive contract', () => {
  it('contains page overflow and reflows every dashboard grid on narrow screens', () => {
    const styles: string = (
      AdminPassportBetaComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: auto');
    expect(styles).toContain('@media (max-width: 640px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('@media (max-height: 520px) and (orientation: landscape)');
  });
});
