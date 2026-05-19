import { Park } from './park';
import { ParkMapPoint } from './park-map-point';

export interface ParkDistanceResponse {
  source: ParkMapPoint;
  distanceUnit: string;
  calculationKind: string;
  targets: ParkDistanceTarget[];
  missingTargetParkIds: string[];
  unavailableTargetParkIds: string[];
}

export interface ParkDistanceTarget {
  proximityRank: number;
  distanceKilometers: number;
  distanceMeters: number;
  distanceUnit: string;
  estimatedTravelDurationMinutes: number;
  park: Park;
}
