import { ParkHistoricalSnapshotPageComponent } from './park-historical-snapshot-page.component';
import { ParkHistoryTimelinePageComponent } from './park-history-timeline-page.component';

describe('park history explorer responsive contract', () => {
  it('contains the canonical timeline inside a 320 pixel viewport', () => {
    const styles: string = componentStyles(ParkHistoryTimelinePageComponent);

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
  });

  it('collapses snapshot controls and cards before they can widen the viewport', () => {
    const styles: string = componentStyles(ParkHistoricalSnapshotPageComponent);

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 620px)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr) !important');
    expect(styles).toContain('.snapshot-date-picker__fields');
  });
});

function componentStyles(component: unknown): string {
  return (component as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
}
