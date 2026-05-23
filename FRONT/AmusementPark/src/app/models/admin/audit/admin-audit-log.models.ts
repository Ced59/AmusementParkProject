import { CollectionResponse } from '@shared/models/contracts';

export interface AdminAuditLog {
  id: string;
  occurredAtUtc: string;
  action: string;
  entityType: string;
  entityId?: string | null;
  actorUserId?: string | null;
  actorEmail?: string | null;
  actorRoles: string[];
  httpMethod: string;
  path: string;
  statusCode: number;
  ipAddress?: string | null;
  userAgent?: string | null;
  traceId: string;
  metadata: Record<string, string>;
}

export interface AdminAuditLogQuery {
  page: number;
  size: number;
  fromUtc?: string | null;
  toUtc?: string | null;
  actorUserId?: string | null;
  actorEmail?: string | null;
  action?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  traceId?: string | null;
}

export interface AdminAuditMetadataEntry {
  key: string;
  value: string;
}

export type AdminAuditLogResponse = CollectionResponse<AdminAuditLog>;
