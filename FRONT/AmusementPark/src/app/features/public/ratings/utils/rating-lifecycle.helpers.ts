import { ParkStatus } from '@app/models/parks/park-status';

export function resolveParkRatingContextHintKey(status: ParkStatus | null | undefined): string | null {
  if (status === 'TemporarilyClosed') {
    return 'ratings.stars.pastVisitHint';
  }

  if (status === 'ClosedDefinitively') {
    return 'ratings.stars.historicalHint';
  }

  return null;
}

export function resolveParkItemRatingContextHintKey(
  parkStatus: ParkStatus | null | undefined,
  itemStatus: string | null | undefined
): string | null {
  const parkHintKey: string | null = resolveParkRatingContextHintKey(parkStatus);
  if (parkHintKey) {
    return parkHintKey;
  }

  const normalizedItemStatus: string = itemStatus?.trim().toLowerCase().replace(/[\s_-]+/g, '') ?? '';
  if (normalizedItemStatus === 'temporarilyclosed'
    || normalizedItemStatus === 'temporaryclosed'
    || normalizedItemStatus === 'closedtemporarily') {
    return 'ratings.stars.pastVisitHint';
  }

  if (normalizedItemStatus === 'closeddefinitively'
    || normalizedItemStatus === 'permanentlyclosed'
    || normalizedItemStatus === 'definitivelyclosed'
    || normalizedItemStatus === 'fermedefinitivement'
    || normalizedItemStatus === 'removed'
    || normalizedItemStatus === 'dismantled') {
    return 'ratings.stars.historicalHint';
  }

  return null;
}
