import { TripCandidateCardComponent } from '../components/trip-candidate-card/trip-candidate-card.component';
import { TripListPageComponent } from './trip-list-page/trip-list-page.component';
import { TripOverviewPageComponent } from './trip-overview-page/trip-overview-page.component';

describe('Trip planning responsive contract', () => {
  it('contains the trip list inside narrow mobile viewports', () => {
    const styles: string = (
      TripListPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('minmax(min(100%, 19rem), 1fr)');
    expect(styles).toContain('grid-template-columns: minmax(0, 1fr)');
    expect(styles).toContain('padding-bottom: calc(5.75rem + env(safe-area-inset-bottom))');
  });

  it('keeps the wishlist, candidate board and days inside the viewport', () => {
    const styles: string = (
      TripOverviewPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('grid-auto-columns: minmax(min(78vw, 10rem), 12rem)');
    expect(styles).toContain('minmax(min(100%, 22rem), 1fr)');
    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('padding-bottom: calc(5.75rem + env(safe-area-inset-bottom))');
  });

  it('reflows each draggable park card without hiding its accessible controls', () => {
    const styles: string = (
      TripCandidateCardComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('grid-template-columns: 5rem minmax(0, 1fr)');
    expect(styles).toContain('flex-wrap: wrap');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('@media (max-width: 36rem)');
  });
});
