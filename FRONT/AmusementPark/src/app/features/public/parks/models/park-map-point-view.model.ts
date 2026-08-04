import { ParkStatus } from '@app/models/parks/park-status';

export interface ParkMapPointViewModel {
  id: string;
  name: string;
  countryCode: string | null;
  countryName: string | null;
  status: ParkStatus;
  city: string | null;
  street: string | null;
  postalCode: string | null;
  latitude: number;
  longitude: number;
  locationLine: string | null;
  addressLine: string | null;
  coordinatesLine: string;
  logoImageId: string | null;
}
