import { AdminParkFitPilotComponent } from './admin-park-fit-pilot.component';

describe('admin Park Fit pilot responsive contract', () => {
  it('contains wide content and reflows every grid on narrow screens', () => {
    const styles: string = (
      AdminParkFitPilotComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-x: auto');
    expect(styles).toContain('@media (max-width: 640px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('@media (max-height: 520px) and (orientation: landscape)');
  });
});
