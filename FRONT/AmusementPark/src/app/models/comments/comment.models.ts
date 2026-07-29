import { LocalizedItem } from '@app/models/shared/localized-item';

export type CommentTargetType = 'Park' | 'ParkItem';
export type CommentAuthorRole = 'Admin' | 'Moderator';

export interface PublicComment {
  id: string;
  targetType: CommentTargetType;
  targetId: string;
  authorDisplayName: string;
  authorRole: CommentAuthorRole;
  bodies: LocalizedItem<string>[];
  isOfficial: boolean;
  canUpdate: boolean;
  canDelete: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CommentSummary {
  targetType: CommentTargetType;
  targetId: string;
  commentCount: number;
  officialComment: PublicComment | null;
}

export interface CommentThread {
  targetType: CommentTargetType;
  targetId: string;
  targetName: string;
  parkId: string;
  parkName: string | null;
  comments: PublicComment[];
}

export interface CreateCommentRequest {
  targetType: CommentTargetType;
  targetId: string;
  bodies: LocalizedItem<string>[];
  isOfficial: boolean;
}

export interface UpdateCommentRequest {
  id: string;
  bodies: LocalizedItem<string>[];
  isOfficial: boolean;
}
