export interface ParkFitGroupProfile {
  profileId: string;
  alias: string;
  heightCentimeters: number | null;
  ageYears: number | null;
  canBeAccompanied: boolean;
  companionAgeYears: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}
