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

export interface ParkDataEditorToken {
  id: string;
  label: string;
  displayPrefix: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  lastUsedAtUtc: string | null;
  revokedAtUtc: string | null;
  revokedByUserId: string | null;
  revocationReason: string | null;
  isActive: boolean;
}

export interface RevokedParkDataEditorTokensResponse {
  revokedCount: number;
}
