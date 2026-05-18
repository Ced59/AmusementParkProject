import { ParkType } from '@app/models/parks/park-type';
import { ParkDetailInfoRowViewModel } from './park-detail-info-row.model';

export interface ParkDetailStatViewModel {
  labelKey: string;
  value: string | number;
  hintKey?: string | null;
  hintText?: string | null;
  tone?: 'primary' | 'lime' | 'sky' | 'gold' | 'rose' | 'soft';
}

export interface ParkDetailViewModel {
  id: string | null;
  name: string;
  countryCode: string | null;
  city: string | null;
  street: string | null;
  postalCode: string | null;
  websiteUrl: string | null;
  logoImageId: string | null;
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
  identityRows: ParkDetailInfoRowViewModel[];
  practicalRows: ParkDetailInfoRowViewModel[];
  publicationRows: ParkDetailInfoRowViewModel[];
  locationRows: ParkDetailInfoRowViewModel[];
  stats: ParkDetailStatViewModel[];
}
