import { AdminTripPilotComponent } from './admin-trip-pilot.component';

describe('AdminTripPilotComponent responsive contract', () => {
  it('keeps indicators and long labels inside narrow admin viewports', () => {
    const styles: string = (
      AdminTripPilotComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });
});
