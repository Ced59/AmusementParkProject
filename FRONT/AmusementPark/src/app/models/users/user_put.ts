export interface UserPut {
  firstName: string;
  lastName: string;
  publicDisplayName?: string | null;
  email: string;
  newEmail: string;
  preferredLanguage: string;
  preferredMeasurementSystem: string;
}
