import { CollectionResponse } from '@shared/models/contracts';

export interface SubmitContactGrievanceRequest {
  message: string;
  website?: string | null;
  languageCode?: string | null;
}

export interface ContactGrievanceSubmission {
  accepted: boolean;
  submittedAtUtc?: string | null;
}

export interface AdminContactGrievance {
  id: string;
  message: string;
  languageCode?: string | null;
  ipAddress: string;
  userAgent?: string | null;
  createdAtUtc: string;
}

export interface AdminContactGrievanceQuery {
  page: number;
  size: number;
  search?: string | null;
  ipAddress?: string | null;
  languageCode?: string | null;
}

export type AdminContactGrievanceResponse = CollectionResponse<AdminContactGrievance>;
