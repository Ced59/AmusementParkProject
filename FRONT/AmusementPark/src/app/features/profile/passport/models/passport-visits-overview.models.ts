import { PassportVisitStatus } from '@app/models/passport/passport-visit.models';

export interface PassportVisitOverviewItemViewModel {
  id: string;
  parkId: string;
  parkName: string | null;
  title: string | null;
  dateLabel: string;
  status: PassportVisitStatus;
  statusLabelKey: string;
  hasPrivateNote: boolean;
}
