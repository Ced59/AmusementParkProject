import { ParkItemCategory } from '../parks/park-item-category';
import { ParkType } from '../parks/park-type';

export interface HomeFeaturedParkMetricModel {
  category: ParkItemCategory;
  count: number;
  labelKey: string;
}

export interface HomeFeaturedParkCardModel {
  id: string | null;
  name: string;
  type: ParkType | null;
  typeLabelKey: string;
  city: string | null;
  countryCode: string | null;
  countryName: string | null;
  locationLine: string | null;
  logoImageId: string | null;
  description: string | null;
  metrics: HomeFeaturedParkMetricModel[];
  isManualFeatured: boolean;
  isSponsoredFeatured: boolean;
  detailLink: string[] | null;
  tone: 'primary' | 'sky' | 'purple';
}
