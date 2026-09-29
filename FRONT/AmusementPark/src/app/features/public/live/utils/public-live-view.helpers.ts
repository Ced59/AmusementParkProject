import { PublicLiveQueue, PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { PublicLiveFilter } from '../models/public-live-filter.model';

const CLOSED_STATUSES: readonly string[] = [
  'Closed',
  'TemporarilyClosed',
  'Down',
  'WeatherClosed',
  'Maintenance',
  'NotOperatingToday',
  'Removed'
];

export function isPublicLiveTargetClosed(target: PublicLiveTarget): boolean {
  return target.availability === 'Current'
    && target.status !== null
    && CLOSED_STATUSES.includes(target.status);
}

export function resolvePublicLiveWaitMinutes(target: PublicLiveTarget): number | null {
  if (target.availability !== 'Current' || isPublicLiveTargetClosed(target)) {
    return null;
  }

  const standbyQueue: PublicLiveQueue | undefined = target.queues.find(
    (queue: PublicLiveQueue) => queue.kind === 'Standby'
  );
  if (standbyQueue) {
    return isPublicLiveQueueWaitUsable(standbyQueue) ? standbyQueue.waitTimeMinutes : null;
  }

  return target.queues.find(isPublicLiveQueueWaitUsable)?.waitTimeMinutes ?? null;
}

export function isPublicLiveQueueWaitUsable(queue: PublicLiveQueue): boolean {
  return queue.waitTimeMinutes !== null
    && (queue.availability === 'Available' || queue.availability === 'Unspecified');
}

export function resolvePublicLiveStatusLabelKey(target: PublicLiveTarget): string {
  if (target.availability !== 'Current') {
    return `liveData.availability.${lowercaseFirst(target.availability)}`;
  }

  return target.status
    ? `liveData.status.${lowercaseFirst(target.status)}`
    : 'liveData.status.unknown';
}

export function resolvePublicLiveTone(target: PublicLiveTarget): 'open' | 'closed' | 'limited' | 'unknown' {
  if (target.availability !== 'Current' || target.status === null || target.status === 'Unknown') {
    return 'unknown';
  }

  if (isPublicLiveTargetClosed(target)) {
    return 'closed';
  }

  return target.status === 'Open' ? 'open' : 'limited';
}

export function filterPublicLiveTargets(
  targets: readonly PublicLiveTarget[],
  filter: PublicLiveFilter
): readonly PublicLiveTarget[] {
  switch (filter) {
    case 'available':
      return targets.filter((target: PublicLiveTarget) => target.availability === 'Current' && !isPublicLiveTargetClosed(target));
    case 'shortWait':
      return targets.filter((target: PublicLiveTarget) => {
        const waitTimeMinutes: number | null = resolvePublicLiveWaitMinutes(target);
        return waitTimeMinutes !== null && waitTimeMinutes <= 20;
      });
    case 'closed':
      return targets.filter(isPublicLiveTargetClosed);
    case 'unknown':
      return targets.filter((target: PublicLiveTarget) =>
        target.availability !== 'Current' || target.status === null || target.status === 'Unknown');
    case 'all':
    default:
      return targets;
  }
}

function lowercaseFirst(value: string): string {
  return value.length > 0 ? `${value.charAt(0).toLowerCase()}${value.slice(1)}` : value;
}
