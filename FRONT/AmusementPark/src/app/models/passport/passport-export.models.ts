export type PassportExportFormat = 'Json' | 'Csv';

export type PassportExportStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed' | 'Expired';

export interface RequestPassportExport {
  format: PassportExportFormat;
}

export interface PassportExport {
  id: string;
  format: PassportExportFormat;
  status: PassportExportStatus;
  schemaVersion: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  expiresAtUtc: string;
  completedAtUtc: string | null;
  fileName: string | null;
  sizeBytes: number | null;
  errorCode: string | null;
  downloadUrl: string | null;
}
