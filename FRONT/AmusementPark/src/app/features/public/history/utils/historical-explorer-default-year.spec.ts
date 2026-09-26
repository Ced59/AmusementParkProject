import { resolveHistoricalExplorerDefaultYear } from './historical-explorer-default-year';

describe('historical explorer default year', () => {
  it('uses the current UTC year independently of a paginated timeline', () => {
    expect(resolveHistoricalExplorerDefaultYear(new Date('2026-12-31T23:59:59Z'))).toBe(2026);
  });
});
