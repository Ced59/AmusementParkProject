import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { CommentsApiService } from '@data-access/comments/comments-api.service';

export interface CommentDataPort {
  getSummary(targetType: CommentTargetType, targetId: string): Observable<CommentSummary>;
  getThread(targetType: CommentTargetType, targetId: string): Observable<CommentThread>;
  createComment(request: CreateCommentRequest): Observable<PublicComment>;
  updateComment(request: UpdateCommentRequest): Observable<PublicComment>;
  deleteComment(commentId: string): Observable<void>;
}

export const COMMENT_DATA_PORT = new InjectionToken<CommentDataPort>('COMMENT_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(CommentsApiService)
});
