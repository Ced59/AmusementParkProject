import { normalizeHistoricalSnapshotDay, resolveHistoricalSnapshotDays } from './history-snapshot-date-selection';

describe('history snapshot date selection', () => {
  it('clears a hidden day when the whole year is selected', () => {
    expect(normalizeHistoricalSnapshotDay(2026, null, 12)).toBeNull();
  });

  it('clears a day that does not exist in the newly selected month', () => {
    expect(normalizeHistoricalSnapshotDay(2026, 2, 31)).toBeNull();
    expect(resolveHistoricalSnapshotDays(2026, 2)).toHaveLength(28);
  });

  it('preserves a valid day and supports leap years', () => {
    expect(normalizeHistoricalSnapshotDay(2028, 2, 29)).toBe(29);
    expect(resolveHistoricalSnapshotDays(2028, 2)).toHaveLength(29);
  });
});
