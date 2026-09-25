export type TripPassportPreference = 'Unknown' | 'MustDo' | 'WantToDo' | 'Optional' | 'NotForMe';

export type TripPassportHistoricalConsistency = 'Verified' | 'Unverified' | 'ConfirmedConflict';

export interface TripPassportTransitionItem {
  parkItemId: string;
  name: string;
  mainImageId: string | null;
  ownPreference: TripPassportPreference;
  historicalConsistency: TripPassportHistoricalConsistency;
}

export interface TripPassportTransitionDay {
  localDate: string;
  parkId: string;
  parkName: string | null;
  isParkAvailable: boolean;
  canConfirm: boolean;
  existingVisitId: string | null;
  existingVisitStatus: 'Draft' | 'Completed' | 'Archived' | null;
  attractions: TripPassportTransitionItem[];
}

export interface TripPassportTransition {
  title: string;
  destinationToday: string;
  days: TripPassportTransitionDay[];
}

export interface ConfirmTripPassportTransitionRequest {
  parkItemIds: string[];
}

export interface ConfirmTripPassportTransitionResult {
  visitId: string;
  wasReplayed: boolean;
  addedRideCount: number;
}
