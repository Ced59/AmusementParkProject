import {
  PassportHistoricalConsistency,
  PassportRideOccurrenceStatus
} from '@app/models/passport/passport-ride-occurrence.models';

export interface ParkItemPassportRideTarget {
  parkItemId: string;
  parkItemName: string;
  parkId: string;
  parkName: string;
  language: string;
}

export interface ParkItemPassportRideVisitOption {
  id: string;
  dateLabel: string;
  title: string | null;
  acceptsLocalTime: boolean;
}

export interface ParkItemPassportRideDraft {
  visitId: string;
  count: number;
  status: PassportRideOccurrenceStatus;
  localTime: string;
  isApproximate: boolean;
  rating: number | null;
  confirmHistoricalConflict: boolean;
}

export interface ParkItemPassportRideEvaluation {
  consistency: PassportHistoricalConsistency;
  openingDate: string | null;
  closingDate: string | null;
}

export type ParkItemPassportRideOutcome =
  | 'rideSaved'
  | 'rideAndRatingSaved'
  | 'rideSavedRatingFailed';
