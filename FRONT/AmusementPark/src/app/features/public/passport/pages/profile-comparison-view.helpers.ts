export function resolveProfileComparisonMissedStatusTranslationKey(
  status: string,
): string {
  return status === 'MissedClosure'
    ? 'passportProfileShare.public.status.MissedClosure'
    : 'passportProfileShare.public.status.MissedOther';
}
