import { Park } from './park';
import { StandaloneAttraction } from '../standalone-attractions/standalone-attraction';
import { LocalizedItem } from '../shared/localized-item';
import { AttractionDetails } from './attraction-details';

type NearbyAttraction = Omit<Partial<StandaloneAttraction>, 'attractionDetails'> & {
  name: string;
  attractionDetails?: Pick<AttractionDetails, 'status'> | null;
};

export type NearbyDestinationKind = 'Park' | 'StandaloneAttraction';

export type NearbyDestination = {
  shortDescriptions?: LocalizedItem<string>[];
  distanceKilometers: number;
  estimatedTravelDurationMinutes: number;
  proximityRank: number;
  photoImageId?: string | null;
} & (
  { kind: 'Park'; park: Park; standaloneAttraction: null }
  | { kind: 'StandaloneAttraction'; park: null; standaloneAttraction: NearbyAttraction }
);
