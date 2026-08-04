import { ParkStatus } from '@app/models/parks/park-status';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';

export interface ParkCardModel {
  id: string | null;
  name: string;
  countryCode: string | null;
  city: string | null;
  status: ParkStatus;
  statusLabelKey: string | null;
  statusIconClass: string | null;
  statusTone: UiPrimitiveTone;
  latitude: number | null;
  longitude: number | null;
  logoImageId: string | null;
  websiteUrl: string | null;
  locationLine: string | null;
  addressLine: string | null;
  coordinatesLine: string | null;
  distanceLine?: string | null;
  travelDurationLine?: string | null;
  shortDescription: string | null;
  isClosedDefinitively: boolean;
  isOpenToVisitors: boolean;
}
