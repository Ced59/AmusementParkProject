import { Park } from './park';
import { StandaloneAttraction } from '../standalone-attractions/standalone-attraction';

export type NearbyDestinationKind = 'Park' | 'StandaloneAttraction';

export type NearbyDestination = {
  distanceKilometers: number;
  estimatedTravelDurationMinutes: number;
  proximityRank: number;
  photoImageId?: string | null;
} & (
  { kind: 'Park'; park: Park; standaloneAttraction: null }
  | { kind: 'StandaloneAttraction'; park: null; standaloneAttraction: StandaloneAttraction }
);
