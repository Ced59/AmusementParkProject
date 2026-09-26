import { PublicHistoricalAmbiguity } from '@app/models/history/public-park-history.models';

export interface HistoricalNameOriginCarrier {
  nameOrigin: string;
}

export function usesCurrentHistoricalSubjectNameFallback(subject: HistoricalNameOriginCarrier): boolean {
  return subject.nameOrigin === 'CurrentFallback';
}

export function buildPublicHistoricalAmbiguityTrackKey(ambiguity: PublicHistoricalAmbiguity): string {
  return [
    ambiguity.subjectType,
    ambiguity.subjectId,
    ambiguity.code,
    ambiguity.attributeKind ?? 'lifecycle'
  ].join(':');
}
