import { Observable, of } from 'rxjs';

import { CommentSummary, CommentTargetType, CommentThread, CreateCommentRequest, PublicComment, UpdateCommentRequest } from '@app/models/comments/comment.models';

import { CommentDataPort } from '../../comment-data.ports';

function createThread(comments: PublicComment[]): CommentThread {
  return {
    targetType: 'Park',
    targetId: 'park-1',
    targetName: 'Demo Park',
    parkId: 'park-1',
    parkName: 'Demo Park',
    comments
  };
}

function createComment(
  id: string,
  isOfficial: boolean,
  createdAtUtc: string,
  canManage: boolean = true
): PublicComment {
  return {
    id,
    targetType: 'Park',
    targetId: 'park-1',
    authorDisplayName: 'Alice',
    authorAvatarUrl: '/images/avatar-1',
    authorRole: 'Admin',
    bodies: [{ languageCode: 'fr', value: `<p>${id}</p>` }],
    isOfficial,
    canUpdate: canManage,
    canDelete: canManage,
    revision: 1,
    createdAtUtc,
    updatedAtUtc: createdAtUtc
  };
}

export class FakeCommentDataPort implements CommentDataPort {
  thread: CommentThread = createThread([
    createComment('regular', false, '2026-07-02T10:00:00Z'),
    createComment('official', true, '2026-07-01T10:00:00Z')
  ]);
  createdComment: PublicComment = createComment('created', false, '2026-07-03T10:00:00Z');
  updatedComment: PublicComment = createComment('regular', true, '2026-07-04T10:00:00Z');
  threadResponse: Observable<CommentThread> | null = null;
  updateResponse: Observable<PublicComment> | null = null;
  deleteResponse: Observable<void> | null = null;
  readonly getThreadCalls: Array<{ targetType: CommentTargetType; targetId: string }> = [];
  readonly createCalls: CreateCommentRequest[] = [];
  readonly updateCalls: UpdateCommentRequest[] = [];
  readonly deleteCalls: Array<{ commentId: string; revision: number }> = [];

  getSummary(
    targetType: CommentTargetType,
    targetId: string,
    languageCode: string
  ): Observable<CommentSummary> {
    return of({
      targetType,
      targetId,
      commentCount: this.thread.comments.length,
      languageCode,
      languageCommentCount: this.thread.comments.length,
      officialComment: this.thread.comments.find((comment: PublicComment) => comment.isOfficial) ?? null
    });
  }

  getThread(targetType: CommentTargetType, targetId: string): Observable<CommentThread> {
    this.getThreadCalls.push({ targetType, targetId });
    return this.threadResponse ?? of(this.thread);
  }

  createComment(request: CreateCommentRequest): Observable<PublicComment> {
    this.createCalls.push(request);
    return of(this.createdComment);
  }

  uploadCommentImage(): Observable<{ id: string; url: string }> {
    return of({
      id: '0123456789abcdef0123456789abcdef',
      url: '/images/0123456789abcdef0123456789abcdef'
    });
  }

  deleteCommentImage(): Observable<void> {
    return of(undefined);
  }

  updateComment(request: UpdateCommentRequest): Observable<PublicComment> {
    this.updateCalls.push(request);
    return this.updateResponse ?? of(this.updatedComment);
  }

  deleteComment(commentId: string, revision: number): Observable<void> {
    this.deleteCalls.push({ commentId, revision });
    return this.deleteResponse ?? of(undefined);
  }
}
