import { LocalizedItem } from '../shared/localized-item';
import { ParkType } from './park-type';
import { ParkStatus } from './park-status';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkOpeningHoursAdminSummary } from './park-opening-hours';

export interface Park {
  id?: string;
  name?: string;
  countryCode?: string;
  type?: ParkType | null;
  status?: ParkStatus;
  founderId?: string | null;
  operatorId?: string | null;
  latitude: number;
  longitude: number;
  isVisible?: boolean;
  adminReviewStatus?: AdminReviewStatus;
  isFeaturedOnHome?: boolean;
  featuredHomeOrder?: number | null;
  isFeaturedOnHomeSponsored?: boolean;
  webSiteUrl?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  currentLogoImageId?: string | null;
  parkItemsTotalCount?: number | null;
  parkItemsVisibleCount?: number | null;
  openingHours?: ParkOpeningHoursAdminSummary | null;
  descriptions?: LocalizedItem<string>[];
}
