import { CommentTargetType } from '@app/models/comments/comment.models';

const COMMENTS_ROOT = 'comments';

export const COMMENTS_API_ENDPOINTS = {
  create: COMMENTS_ROOT,
  uploadImage: `${COMMENTS_ROOT}/images`,
  deleteImage: (imageId: string): string =>
    `${COMMENTS_ROOT}/images/${encodeURIComponent(imageId)}`,
  update: (commentId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(commentId)}`,
  delete: (commentId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(commentId)}`,
  getSummary: (targetType: CommentTargetType, targetId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}/summary`,
  getThread: (targetType: CommentTargetType, targetId: string): string =>
    `${COMMENTS_ROOT}/${encodeURIComponent(targetType)}/${encodeURIComponent(targetId)}`
} as const;
