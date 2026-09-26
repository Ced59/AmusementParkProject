export function normalizeHistoricalSnapshotDay(
  year: number,
  month: number | null,
  day: number | null
): number | null {
  if (
    month === null
    || day === null
    || !Number.isInteger(year)
    || !Number.isInteger(month)
    || month < 1
    || month > 12
    || !Number.isInteger(day)
  ) {
    return null;
  }

  const lastDay: number = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return day >= 1 && day <= lastDay ? day : null;
}

export function resolveHistoricalSnapshotDays(year: number, month: number | null): number[] {
  if (month === null) {
    return Array.from({ length: 31 }, (_value: unknown, index: number): number => index + 1);
  }

  if (!Number.isInteger(year) || !Number.isInteger(month) || month < 1 || month > 12) {
    return [];
  }

  const dayCount: number = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return Array.from({ length: dayCount }, (_value: unknown, index: number): number => index + 1);
}
