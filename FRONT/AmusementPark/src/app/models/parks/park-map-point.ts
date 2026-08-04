import { ParkAudienceClassification } from './park-audience-classification';
import { ParkStatus } from './park-status';

export interface ParkMapPoint {
  id: string;
  name: string;
  countryCode?: string | null;
  audienceClassification?: ParkAudienceClassification | null;
  status?: ParkStatus | null;
  city?: string | null;
  street?: string | null;
  postalCode?: string | null;
  latitude: number;
  longitude: number;
  currentLogoImageId?: string | null;
}
