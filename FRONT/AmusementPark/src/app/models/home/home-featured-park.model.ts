import { LocalizedItem } from '../shared/localized-item';
import { ParkType } from '../parks/park-type';
import { ParkItemCategory } from '../parks/park-item-category';

export interface HomeFeaturedParkCategoryCountModel {
  category: ParkItemCategory;
  count: number;
}

export interface HomeFeaturedParkModel {
  id: string | null;
  name: string;
  countryCode: string | null;
  type: ParkType | null;
  latitude: number;
  longitude: number;
  descriptions: LocalizedItem<string>[];
  city: string | null;
  currentLogoImageId: string | null;
  isManualFeatured: boolean;
  isSponsoredFeatured: boolean;
  countsByCategory: HomeFeaturedParkCategoryCountModel[];
}
