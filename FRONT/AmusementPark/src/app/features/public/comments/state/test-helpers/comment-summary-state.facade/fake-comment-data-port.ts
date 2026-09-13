import { Observable, of } from 'rxjs';

import { CommentSummary, CommentTargetType, CommentThread, CreateCommentRequest, PublicComment, UpdateCommentRequest } from '@app/models/comments/comment.models';

import { CommentImageUpload } from '@app/models/comments/comment-image.models';

import { CommentDataPort } from '../../comment-data.ports';

export class FakeCommentDataPort implements CommentDataPort {
  readonly summaryCalls: Array<{
    targetType: CommentTargetType;
    targetId: string;
    languageCode: string;
  }> = [];

  getSummary(
    targetType: CommentTargetType,
    targetId: string,
    languageCode: string
  ): Observable<CommentSummary> {
    this.summaryCalls.push({ targetType, targetId, languageCode });
    return of({
      targetType,
      targetId,
      commentCount: 2,
      languageCode,
      languageCommentCount: languageCode === 'fr' ? 1 : 0,
      officialComment: null
    });
  }

  getThread(): Observable<CommentThread> {
    throw new Error('Not used.');
  }

  createComment(_request: CreateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  uploadCommentImage(_file: File): Observable<CommentImageUpload> {
    throw new Error('Not used.');
  }

  deleteCommentImage(_imageId: string): Observable<void> {
    throw new Error('Not used.');
  }

  updateComment(_request: UpdateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  deleteComment(_commentId: string, _revision: number): Observable<void> {
    throw new Error('Not used.');
  }
}
