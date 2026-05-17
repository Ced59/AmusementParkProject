import { LocalizedItem } from '../shared/localized-item';
import { ParkType } from './park-type';

export interface Park {
  id?: string;
  name?: string;
  countryCode?: string;
  type?: ParkType | null;
  founderId?: string | null;
  operatorId?: string | null;
  latitude: number;
  longitude: number;
  isVisible?: boolean;
  isFeaturedOnHome?: boolean;
  featuredHomeOrder?: number | null;
  isFeaturedOnHomeSponsored?: boolean;
  webSiteUrl?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  currentLogoImageId?: string | null;
  descriptions?: LocalizedItem<string>[];
}
