import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';

export interface ParkItemAdminRow {
  id: string;
  parkId: string;
  parkName: string;
  name: string;
  category: ParkItemCategory;
  type: ParkItemType;
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
}
