import { ImageCategory } from '@app/models/images/image-category';
import { ParkType } from '@app/models/parks/park-type';
import { ParkDetailInfoRowViewModel } from './park-detail-info-row.model';

export interface ParkDetailStatViewModel {
  labelKey: string;
  value: string | number;
  hintKey?: string | null;
  hintText?: string | null;
  tone?: 'primary' | 'lime' | 'sky' | 'gold' | 'rose' | 'soft';
}


export interface ParkDetailPhotoCategoryOptionViewModel {
  key: string;
  labelKey: string;
  count: number;
}

export interface ParkDetailPhotoViewModel {
  id: string;
  imageId: string;
  category: ImageCategory;
  categoryKey: string;
  categoryLabelKey: string;
  description: string | null;
  alt: string;
  isCurrent: boolean;
  sourceTitle?: string | null;
  sourceSubtitle?: string | null;
  sourceIconClass?: string | null;
  sourceRouterLink?: string[] | null;
  sourceLinkLabelKey?: string | null;
}

export interface ParkDetailViewModel {
  id: string | null;
  name: string;
  countryCode: string | null;
  countryName: string | null;
  city: string | null;
  street: string | null;
  postalCode: string | null;
  websiteUrl: string | null;
  logoImageId: string | null;
  heroImageId: string | null;
  description: string | null;
  type: ParkType | null;
  typeLabelKey: string | null;
  founderId: string | null;
  founderName: string | null;
  operatorId: string | null;
  operatorName: string | null;
  isVisible: boolean | null;
  isFeaturedOnHome: boolean | null;
  featuredHomeOrder: number | null;
  isFeaturedOnHomeSponsored: boolean | null;
  locationLine: string | null;
  addressLine: string | null;
  latitude: number | null;
  longitude: number | null;
  hasPracticalInfo: boolean;
  hasLocationInfo: boolean;
  hasDescription: boolean;
  exploreLink: string[] | null;
  zonesLink: string[] | null;
  imagesLink: string[] | null;
  mapLink: string[] | null;
  identityRows: ParkDetailInfoRowViewModel[];
  practicalRows: ParkDetailInfoRowViewModel[];
  publicationRows: ParkDetailInfoRowViewModel[];
  locationRows: ParkDetailInfoRowViewModel[];
  stats: ParkDetailStatViewModel[];
  photos: ParkDetailPhotoViewModel[];
  photoCategories: ParkDetailPhotoCategoryOptionViewModel[];
}
