import { AdminWatchPilotComponent } from './admin-watch-pilot.component';

describe('AdminWatchPilotComponent responsive contract', (): void => {
  it('keeps every narrow layout column shrinkable', (): void => {
    const styles: string = (
      AdminWatchPilotComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');
    expect(styles).toContain('minmax(0, 1fr)');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: auto');
  });
});
