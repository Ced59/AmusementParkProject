import { PublicHistoricalSubjectSnapshot } from '@app/models/history/public-park-history.models';

export function usesCurrentHistoricalSubjectNameFallback(subject: PublicHistoricalSubjectSnapshot): boolean {
  return subject.nameOrigin === 'CurrentFallback';
}
