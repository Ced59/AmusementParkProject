import { DestroyRef } from '@angular/core';
import { Observable, of } from 'rxjs';

import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { AuthService } from '@app/services/auth/auth.service';
import { CommentImageUpload } from '@app/models/comments/comment-image.models';
import { CommentDataPort } from './comment-data.ports';
import { CommentSummaryStateFacade } from './comment-summary-state.facade';

class FakeDestroyRef implements DestroyRef {
  readonly destroyed: boolean = false;

  onDestroy(_callback: () => void): () => void {
    return (): void => undefined;
  }
}

class FakeAuthService {
  ensureValidAccessToken(): Observable<string | null> {
    return of(null);
  }

  hasRole(): boolean {
    return false;
  }
}

class FakeCommentDataPort implements CommentDataPort {
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

describe('CommentSummaryStateFacade', () => {
  it('reloads the same target when the current language changes', () => {
    const dataPort: FakeCommentDataPort = new FakeCommentDataPort();
    const facade: CommentSummaryStateFacade = new CommentSummaryStateFacade(
      dataPort,
      new FakeAuthService() as unknown as AuthService,
      new FakeDestroyRef()
    );

    facade.load('Park', ' park-1 ', 'FR');
    expect(facade.summary()?.languageCode).toBe('fr');

    facade.load('Park', 'park-1', 'en');

    expect(dataPort.summaryCalls).toEqual([
      { targetType: 'Park', targetId: 'park-1', languageCode: 'fr' },
      { targetType: 'Park', targetId: 'park-1', languageCode: 'en' }
    ]);
    expect(facade.summary()).toEqual(expect.objectContaining({
      targetType: 'Park',
      targetId: 'park-1',
      languageCode: 'en',
      languageCommentCount: 0
    }));
  });
});
