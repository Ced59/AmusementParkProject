import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { AttractionDetails } from '@app/models/parks/attraction-details';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { LocalizedItem } from '@app/models/shared/localized-item';

export interface StandaloneAttraction {
  id?: string | null;
  name: string;
  countryCode?: string | null;
  type: ParkItemType;
  subtype?: string | null;
  operatorId?: string | null;
  websiteUrl?: string | null;
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  descriptions?: LocalizedItem<string>[];
  attractionDetails?: AttractionDetails | null;
  attractionLocations?: AttractionLocations | null;
  isVisible: boolean;
  adminReviewStatus: AdminReviewStatus;
  legacyParkId?: string | null;
  legacyParkItemId?: string | null;
}

export interface StandaloneAttractionMigrationRequest {
  legacyParkId: string;
  legacyParkItemId?: string | null;
  targetStandaloneAttractionId?: string | null;
  retireLegacyPark: boolean;
  retireLegacyParkItem: boolean;
}
