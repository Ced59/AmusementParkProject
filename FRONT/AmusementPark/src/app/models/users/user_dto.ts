export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  publicDisplayName?: string | null;
  publicIdentifier?: string | null;
  isActivated: boolean;
  isBlocked: boolean;
  roles: string[];
  preferredLanguage: string;
  preferredMeasurementSystem?: string | null;
  avatarUrl: string;
  createdAt: string;
  updatedAt: string;
}
