import { resolveDisplayedHistoryPageCount } from './history-pagination-display';

describe('history pagination display', () => {
  it('displays one page for a valid empty timeline', () => {
    expect(resolveDisplayedHistoryPageCount(0)).toBe(1);
  });

  it('preserves the real page count when events are paginated', () => {
    expect(resolveDisplayedHistoryPageCount(4)).toBe(4);
  });
});
