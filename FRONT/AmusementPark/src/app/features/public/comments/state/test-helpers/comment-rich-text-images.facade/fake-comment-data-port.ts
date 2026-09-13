import { Observable, Subject, of } from 'rxjs';

import { CommentImageUpload } from '@app/models/comments/comment-image.models';

import { CommentSummary, CommentTargetType, CommentThread, CreateCommentRequest, PublicComment, UpdateCommentRequest } from '@app/models/comments/comment.models';

import { CommentDataPort } from '../../comment-data.ports';

export class FakeCommentDataPort implements CommentDataPort {
  readonly uploadSubjects: Subject<CommentImageUpload>[] = [];
  readonly uploadedFiles: File[] = [];
  readonly deletedImageIds: string[] = [];

  getSummary(
    _targetType: CommentTargetType,
    _targetId: string,
    _languageCode: string
  ): Observable<CommentSummary> {
    throw new Error('Not used.');
  }

  getThread(_targetType: CommentTargetType, _targetId: string): Observable<CommentThread> {
    throw new Error('Not used.');
  }

  createComment(_request: CreateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  updateComment(_request: UpdateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  deleteComment(_commentId: string, _revision: number): Observable<void> {
    throw new Error('Not used.');
  }

  uploadCommentImage(file: File): Observable<CommentImageUpload> {
    this.uploadedFiles.push(file);
    const subject: Subject<CommentImageUpload> = new Subject<CommentImageUpload>();
    this.uploadSubjects.push(subject);
    return subject.asObservable();
  }

  deleteCommentImage(imageId: string): Observable<void> {
    this.deletedImageIds.push(imageId);
    return of(undefined);
  }
}
