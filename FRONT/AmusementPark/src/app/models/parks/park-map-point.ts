export interface ParkMapPoint {
  id: string;
  name: string;
  countryCode?: string | null;
  city?: string | null;
  street?: string | null;
  postalCode?: string | null;
  latitude: number;
  longitude: number;
  currentLogoImageId?: string | null;
}
