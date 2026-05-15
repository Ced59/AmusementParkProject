export interface ParkCardModel {
  id: string | null;
  name: string;
  countryCode: string | null;
  city: string | null;
  latitude: number | null;
  longitude: number | null;
  logoImageId: string | null;
  websiteUrl: string | null;
  locationLine: string | null;
  addressLine: string | null;
  coordinatesLine: string | null;
  shortDescription: string | null;
}
