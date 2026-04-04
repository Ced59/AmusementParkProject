import { AttractionAccessCondition } from './attraction-access-condition';
import { AttractionWaterExposureLevel } from './attraction-water-exposure-level';

export interface AttractionDetails {
  manufacturerId?: string | null;
  model?: string | null;
  externalSource?: string | null;
  externalId?: string | null;
  sourceUrl?: string | null;
  status?: string | null;
  materialType?: string | null;
  seatingType?: string | null;
  launchType?: string | null;
  restraintType?: string | null;
  isLaunched?: boolean | null;
  openingDate?: string | null;
  closingDate?: string | null;
  openingDateText?: string | null;
  closingDateText?: string | null;
  durationInSeconds?: number | null;
  capacityPerHour?: number | null;
  heightInFeet?: number | null;
  heightInMeters?: number | null;
  lengthInFeet?: number | null;
  lengthInMeters?: number | null;
  speedInMph?: number | null;
  speedInKmH?: number | null;
  dropInMeters?: number | null;
  inversionCount?: number | null;
  trainCount?: number | null;
  carsPerTrain?: number | null;
  ridersPerVehicle?: number | null;
  hasSingleRider?: boolean | null;
  hasFastPass?: boolean | null;
  isAccessibleForReducedMobility?: boolean | null;
  isIndoor?: boolean | null;
  waterExposureLevel?: AttractionWaterExposureLevel | null;
  accessConditions?: AttractionAccessCondition[] | null;
}
