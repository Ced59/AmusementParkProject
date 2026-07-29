import { CommentTargetType } from '@app/models/comments/comment.models';

const COMMENTS_ROOT = 'comments';

export const COMMENTS_API_ENDPOINTS = {
  create: COMMENTS_ROOT,
  update: (commentId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(commentId)}`,
  delete: (commentId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(commentId)}`,
  getSummary: (targetType: CommentTargetType, targetId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/summary`,
  getThread: (targetType: CommentTargetType, targetId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}`
} as const;
