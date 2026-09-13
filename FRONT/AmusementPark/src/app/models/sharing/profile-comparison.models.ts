import { ProfileComparisonCategory } from './profile-comparison-invitation.models';

export type ProfileComparisonRatingAffinity = 'Close' | 'Neutral' | 'Divergent';

export interface ProfileComparisonPark {
  name: string;
  countryCode: string | null;
  creatorVisitCount: number | null;
  acceptorVisitCount: number | null;
}

export interface ProfileComparisonRating {
  targetType: string;
  name: string;
  parkName: string | null;
  category: string | null;
  creatorRating: number;
  acceptorRating: number;
  absoluteDifference: number;
  affinity: ProfileComparisonRatingAffinity;
}

export interface ProfileComparisonYear {
  year: number;
  creatorVisitCount: number;
  acceptorVisitCount: number;
  creatorRideCount: number | null;
  acceptorRideCount: number | null;
}

export interface ProfileComparisonMissedItem {
  name: string;
  status: string;
  creatorOccurrenceCount: number | null;
  acceptorOccurrenceCount: number | null;
}

export interface SharedProfileComparison {
  createdAtUtc: string;
  creatorDisplayName: string | null;
  acceptorDisplayName: string | null;
  categories: ProfileComparisonCategory[];
  parks: ProfileComparisonPark[];
  ratings: ProfileComparisonRating[];
  years: ProfileComparisonYear[];
  missedItems: ProfileComparisonMissedItem[];
  commonRatingCount: number;
  minimumRatingsForCorrelation: number;
  ratingCorrelation: number | null;
  hasIncompleteCatalog: boolean;
  calculationVersion: string;
}

export interface ProfileComparisonSummary {
  shareId: string;
  otherDisplayName: string | null;
  createdAtUtc: string;
  categories: ProfileComparisonCategory[];
  isModerationSuspended: boolean;
}

export interface ProfileComparisonRevocation {
  revokedAtUtc: string;
}
