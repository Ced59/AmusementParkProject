import { AdminHistoricalSubject } from '@app/models/history/admin-historical-workbench.models';

export function filterHistoricalFactSubjectsForPark(
  subjects: readonly AdminHistoricalSubject[],
  parkId: string
): readonly AdminHistoricalSubject[] {
  const normalizedParkId: string = parkId.trim();
  return subjects.filter((subject: AdminHistoricalSubject): boolean =>
    subject.contextParkId === normalizedParkId
    || (subject.type === 'Park' && subject.id === normalizedParkId)
  );
}
