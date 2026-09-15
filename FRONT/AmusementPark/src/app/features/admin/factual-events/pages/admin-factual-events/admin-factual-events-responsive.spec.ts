import { AdminFactualEventsComponent } from './admin-factual-events.component';

describe('AdminFactualEventsComponent responsive contract', (): void => {
  it('prevents card and control overflow at narrow viewport widths', (): void => {
    const styles: string = (AdminFactualEventsComponent as unknown as {
      ɵcmp: { styles: string[] };
    }).ɵcmp.styles.join('\n');

    expect(styles).toContain('minmax(min(100%, 27rem), 1fr)');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-wrap: anywhere');
    expect(styles).toContain('.factual-event-card__windows');
    expect(styles).toContain('flex-wrap: wrap');
    expect(styles).toContain('@media (max-width: 48rem)');
    expect(styles).toContain('@media (max-width: 36rem)');
    expect(styles).toContain('@media (max-width: 30rem)');
  });
});
