import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { PassportVisitOverviewItemViewModel } from '../models/passport-visits-overview.models';
import { formatPassportVisitDate } from './passport-visit-date.mapper';

export function mapPassportVisitOverviewItem(
  visit: PassportVisit,
  language: string
): PassportVisitOverviewItemViewModel {
  return {
    id: visit.id,
    parkId: visit.parkId,
    parkName: visit.parkName?.trim() || visit.parkId,
    title: visit.title?.trim() || null,
    dateLabel: formatPassportVisitDate(visit.date, language),
    status: visit.status,
    statusLabelKey: `passport.overview.status.${visit.status}`,
    hasPrivateNote: visit.hasPrivateNote ?? Boolean(visit.privateNote?.trim())
  };
}
