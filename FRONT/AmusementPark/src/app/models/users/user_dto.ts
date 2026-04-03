import { AppRole } from './app-role';

export interface UserDto {
  email: string;
  firstName: string | null;
  lastName: string | null;
  isActivated: boolean;
  isBlocked: boolean;
  roles: AppRole[];
  preferredLanguage: string | null;
  avatarUrl: string | null;
  id: string;
  createdAt: string;
  updatedAt: string;
}
