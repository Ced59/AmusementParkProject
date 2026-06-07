import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';

export interface AdminParkItemWorkbenchCoordinates {
  latitude: number;
  longitude: number;
}

export interface AdminParkItemQuickCreateDraft {
  parkId: string;
  zoneId?: string | null;
  name: string;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  manufacturerId?: string | null;
  coordinates?: AdminParkItemWorkbenchCoordinates | null;
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
}

export interface AdminParkItemInlineAdministrationDraft {
  zoneId?: string | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  manufacturerId?: string | null;
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
}

export const ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS: Pick<ParkItem, 'category' | 'type' | 'isVisible' | 'adminReviewStatus'> = {
  category: 'Attraction',
  type: 'Attraction',
  isVisible: false,
  adminReviewStatus: 'ToReview'
};
