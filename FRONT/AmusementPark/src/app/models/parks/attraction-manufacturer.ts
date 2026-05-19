import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { LocalizedItem } from '../shared/localized-item';

export interface AttractionManufacturer {
  id?: string;
  name: string;
  biography?: LocalizedItem<string>[];
  attractionCount?: number;
  adminReviewStatus?: AdminReviewStatus;
}
