import { AppRole } from './app-role';

export interface UserRolesUpdateResponse {
  userId: string;
  roles: AppRole[];
}

export interface UserLockStateResponse {
  userId: string;
  firstName: string | null;
  lastName: string | null;
}
