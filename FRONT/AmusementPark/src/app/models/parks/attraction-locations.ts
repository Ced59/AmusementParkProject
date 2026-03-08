import { AttractionLocationPoint } from './attraction-location-point';

export interface AttractionLocations {
  entrance?: AttractionLocationPoint | null;
  exit?: AttractionLocationPoint | null;
  fastPassEntrance?: AttractionLocationPoint | null;
  reducedMobilityEntrance?: AttractionLocationPoint | null;
}
