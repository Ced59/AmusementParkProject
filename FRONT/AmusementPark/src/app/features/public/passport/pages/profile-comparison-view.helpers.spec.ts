import { resolveProfileComparisonMissedStatusTranslationKey } from './profile-comparison-view.helpers';

describe('profile comparison view helpers', () => {
  it('maps closure and other missed statuses to localized visitor labels', () => {
    expect(
      resolveProfileComparisonMissedStatusTranslationKey('MissedClosure'),
    ).toBe('passportProfileShare.public.status.MissedClosure');
    expect(
      resolveProfileComparisonMissedStatusTranslationKey('MissedOther'),
    ).toBe('passportProfileShare.public.status.MissedOther');
  });

  it('falls back to a visitor label without exposing an unknown technical status', () => {
    expect(
      resolveProfileComparisonMissedStatusTranslationKey('legacy-status'),
    ).toBe('passportProfileShare.public.status.MissedOther');
  });
});
