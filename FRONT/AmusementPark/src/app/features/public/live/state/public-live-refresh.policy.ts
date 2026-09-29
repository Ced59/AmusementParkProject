import { PublicLiveTarget } from '@app/models/live-data/public-live.models';

const MINIMUM_REFRESH_DELAY_MILLISECONDS: number = 30_000;
const MAXIMUM_CURRENT_REFRESH_DELAY_MILLISECONDS: number = 120_000;
const UNAVAILABLE_REFRESH_DELAY_MILLISECONDS: number = 120_000;
const EMPTY_REFRESH_DELAY_MILLISECONDS: number = 300_000;

export function resolvePublicLiveRefreshDelay(
  target: PublicLiveTarget | null,
  items: readonly PublicLiveTarget[],
  nowMilliseconds: number = Date.now()
): number {
  const currentTargets: readonly PublicLiveTarget[] = [target, ...items]
    .filter((candidate: PublicLiveTarget | null): candidate is PublicLiveTarget => candidate?.availability === 'Current');

  if (currentTargets.length === 0) {
    return target === null && items.length === 0
      ? EMPTY_REFRESH_DELAY_MILLISECONDS
      : UNAVAILABLE_REFRESH_DELAY_MILLISECONDS;
  }

  const remainingLifetimes: number[] = currentTargets
    .map((candidate: PublicLiveTarget) => candidate.expiresAtUtc ? Date.parse(candidate.expiresAtUtc) - nowMilliseconds : Number.NaN)
    .filter((remaining: number) => Number.isFinite(remaining) && remaining > 0);

  if (remainingLifetimes.length === 0) {
    return MINIMUM_REFRESH_DELAY_MILLISECONDS;
  }

  const shortestRemainingLifetime: number = Math.min(...remainingLifetimes);
  return Math.max(
    MINIMUM_REFRESH_DELAY_MILLISECONDS,
    Math.min(MAXIMUM_CURRENT_REFRESH_DELAY_MILLISECONDS, Math.floor(shortestRemainingLifetime / 2))
  );
}
