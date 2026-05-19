import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { LocalizedItem } from '../shared/localized-item';

export interface ParkOperator {
  id?: string;
  name: string;
  description?: LocalizedItem<string>[];
  adminReviewStatus?: AdminReviewStatus;
}
