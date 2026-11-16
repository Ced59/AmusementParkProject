import { LocalizedItem } from '../shared/localized-item';
import { ParkStatus } from './park-status';
import { NearbyDestinationKind } from './nearby-destination';

export interface NearbyDestinationCardResponse {
  kind: NearbyDestinationKind;
  id: string;
  name: string | null;
  countryCode: string | null;
  city: string | null;
  street: string | null;
  postalCode: string | null;
  latitude: number | null;
  longitude: number | null;
  websiteUrl: string | null;
  logoImageId: string | null;
  photoImageId: string | null;
  parkStatus: ParkStatus | null;
  attractionStatus: string | null;
  shortDescriptions: LocalizedItem<string>[];
  distanceKilometers: number;
  estimatedTravelDurationMinutes: number;
  proximityRank: number;
}
