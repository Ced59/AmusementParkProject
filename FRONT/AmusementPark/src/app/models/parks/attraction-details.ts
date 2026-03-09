import { AttractionAccessCondition } from './attraction-access-condition';
import { AttractionWaterExposureLevel } from './attraction-water-exposure-level';

export interface AttractionDetails {
  manufacturerId?: string | null;
  model?: string | null;
  openingDate?: string | null;
  closingDate?: string | null;
  durationInSeconds?: number | null;
  capacityPerHour?: number | null;
  heightInMeters?: number | null;
  lengthInMeters?: number | null;
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
