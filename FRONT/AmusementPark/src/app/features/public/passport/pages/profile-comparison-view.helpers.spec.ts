import {
  resolveProfileComparisonCorrelationTranslationKey,
  resolveProfileComparisonMissedStatusTranslationKey,
} from './profile-comparison-view.helpers';

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

  it('distinguishes missing volume from ratings without enough variance', () => {
    expect(resolveProfileComparisonCorrelationTranslationKey(null, 4, 5)).toBe(
      'profileComparison.result.correlation.pending',
    );
    expect(resolveProfileComparisonCorrelationTranslationKey(null, 5, 5)).toBe(
      'profileComparison.result.correlation.noVariance',
    );
  });

  it('maps calculated correlations to qualitative visitor labels', () => {
    expect(resolveProfileComparisonCorrelationTranslationKey(0.65, 5, 5)).toBe(
      'profileComparison.result.correlation.aligned',
    );
    expect(resolveProfileComparisonCorrelationTranslationKey(-0.65, 5, 5)).toBe(
      'profileComparison.result.correlation.contrasting',
    );
    expect(resolveProfileComparisonCorrelationTranslationKey(0.2, 5, 5)).toBe(
      'profileComparison.result.correlation.mixed',
    );
  });
});
