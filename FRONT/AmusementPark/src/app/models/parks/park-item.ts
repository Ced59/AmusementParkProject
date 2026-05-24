import { LocalizedItem } from '../shared/localized-item';
import { AttractionDetails } from './attraction-details';
import { AttractionLocations } from './attraction-locations';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from './park-item-category';
import { ParkItemType } from './park-item-type';

export interface ParkItem {
  id?: string;
  parkId: string;
  zoneId?: string | null;
  name: string;
  category: ParkItemCategory;
  type: ParkItemType;
  subtype?: string | null;
  latitude: number;
  longitude: number;
  descriptions?: LocalizedItem<string>[];
  attractionDetails?: AttractionDetails | null;
  attractionLocations?: AttractionLocations | null;
  isVisible?: boolean;
  adminReviewStatus?: AdminReviewStatus;
}
