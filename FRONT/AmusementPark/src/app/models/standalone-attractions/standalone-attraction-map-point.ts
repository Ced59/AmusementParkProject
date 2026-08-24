import { ParkItemType } from '@app/models/parks/park-item-type';

export interface StandaloneAttractionMapPoint {
  id: string;
  name: string;
  countryCode: string | null;
  type: ParkItemType;
  subtype: string | null;
  status: string | null;
  city: string | null;
  street: string | null;
  postalCode: string | null;
  latitude: number;
  longitude: number;
}
