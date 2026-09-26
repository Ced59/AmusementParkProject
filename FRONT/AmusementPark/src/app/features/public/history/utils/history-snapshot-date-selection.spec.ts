import {
  isValidHistoricalSnapshotDate,
  normalizeHistoricalSnapshotDay,
  resolveHistoricalSnapshotDays
} from './history-snapshot-date-selection';

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

  it('validates the combined civil date without inventing missing parts', () => {
    expect(isValidHistoricalSnapshotDate(2025, 2, 31)).toBe(false);
    expect(isValidHistoricalSnapshotDate(2028, 2, 29)).toBe(true);
    expect(isValidHistoricalSnapshotDate(2025, 2, null)).toBe(true);
    expect(isValidHistoricalSnapshotDate(2025, null, null)).toBe(true);
    expect(isValidHistoricalSnapshotDate(2025, null, 12)).toBe(false);
  });
});
