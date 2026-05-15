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
  locationLine: string | null;
  addressLine: string | null;
  latitude: number | null;
  longitude: number | null;
  hasPracticalInfo: boolean;
  hasLocationInfo: boolean;
  exploreLink: string[] | null;
}
