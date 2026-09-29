import { isValidLiveOperationalReason } from './live-operational-reason.validator';

describe('isValidLiveOperationalReason', () => {
  it('accepts a trimmed single-line operational reason', () => {
    expect(isValidLiveOperationalReason('  Incident fournisseur  ')).toBe(true);
  });

  it('rejects line breaks and other control characters before submission', () => {
    expect(isValidLiveOperationalReason('Incident\nfournisseur')).toBe(false);
    expect(isValidLiveOperationalReason('Incident\tfournisseur')).toBe(false);
  });

  it('matches the domain length boundaries after trimming', () => {
    expect(isValidLiveOperationalReason('ab')).toBe(false);
    expect(isValidLiveOperationalReason('a'.repeat(500))).toBe(true);
    expect(isValidLiveOperationalReason('a'.repeat(501))).toBe(false);
  });
});
