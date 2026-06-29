import { ImageCategory } from '@app/models/images/image-category';
import { ParkType } from '@app/models/parks/park-type';
import { RatingSummary } from '@app/models/ratings/rating.models';
import { UiPhotoCarouselImage } from '@ui/media';
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

export interface ParkDetailPhotoViewModel extends UiPhotoCarouselImage {
  category: ImageCategory;
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
  rating: RatingSummary | null;
  exploreLink: string[] | null;
  zonesLink: string[] | null;
  imagesLink: string[] | null;
  videosLink: string[] | null;
  mapLink: string[] | null;
  weatherLink: string[] | null;
  openingHoursLink: string[] | null;
  primaryPhoto: ParkDetailPhotoViewModel | null;
  identityRows: ParkDetailInfoRowViewModel[];
  practicalRows: ParkDetailInfoRowViewModel[];
  publicationRows: ParkDetailInfoRowViewModel[];
  locationRows: ParkDetailInfoRowViewModel[];
  stats: ParkDetailStatViewModel[];
  photos: ParkDetailPhotoViewModel[];
  photoCategories: ParkDetailPhotoCategoryOptionViewModel[];
}
