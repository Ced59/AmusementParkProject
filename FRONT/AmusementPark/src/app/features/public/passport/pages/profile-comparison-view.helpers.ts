export function resolveProfileComparisonMissedStatusTranslationKey(
  status: string,
): string {
  return status === 'MissedClosure'
    ? 'passportProfileShare.public.status.MissedClosure'
    : 'passportProfileShare.public.status.MissedOther';
}

export function resolveProfileComparisonCorrelationTranslationKey(
  correlation: number | null,
  commonRatingCount: number,
  minimumRatingsForCorrelation: number,
): string {
  if (correlation == null) {
    return commonRatingCount >= minimumRatingsForCorrelation
      ? 'profileComparison.result.correlation.noVariance'
      : 'profileComparison.result.correlation.pending';
  }
  if (correlation >= 0.65) {
    return 'profileComparison.result.correlation.aligned';
  }
  if (correlation <= -0.65) {
    return 'profileComparison.result.correlation.contrasting';
  }
  return 'profileComparison.result.correlation.mixed';
}
