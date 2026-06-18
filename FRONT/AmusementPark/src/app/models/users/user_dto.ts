export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActivated: boolean;
  isBlocked: boolean;
  roles: string[];
  preferredLanguage: string;
  preferredMeasurementSystem?: string | null;
  avatarUrl: string;
  createdAt: string;
  updatedAt: string;
}
