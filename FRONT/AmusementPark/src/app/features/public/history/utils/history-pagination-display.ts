export function resolveDisplayedHistoryPageCount(totalPages: number): number {
  return Number.isInteger(totalPages) && totalPages > 0 ? totalPages : 1;
}
