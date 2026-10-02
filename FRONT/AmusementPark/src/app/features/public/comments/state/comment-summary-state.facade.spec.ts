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
import { FakeDestroyRef } from './test-helpers/comment-summary-state.facade/fake-destroy-ref';
import { FakeAuthService } from './test-helpers/comment-summary-state.facade/fake-auth-service';
import { FakeCommentDataPort } from './test-helpers/comment-summary-state.facade/fake-comment-data-port';

describe('CommentSummaryStateFacade', () => {
  it('does not force a refresh attempt for an anonymous public visitor', () => {
    const authService: FakeAuthService = new FakeAuthService();
    const sessionSpy = vi.spyOn(authService, 'ensureValidAccessToken');
    const facade: CommentSummaryStateFacade = new CommentSummaryStateFacade(
      new FakeCommentDataPort(),
      authService as unknown as AuthService,
      new FakeDestroyRef()
    );

    facade.initializeAuthorAccess();

    expect(sessionSpy).toHaveBeenCalledWith(false);
    expect(facade.canWrite()).toBe(false);
  });

  it('retains author access for a valid moderator session', () => {
    const authService: FakeAuthService = new FakeAuthService();
    vi.spyOn(authService, 'ensureValidAccessToken').mockReturnValue(of('token'));
    vi.spyOn(authService, 'hasRole').mockReturnValue(true);
    const facade: CommentSummaryStateFacade = new CommentSummaryStateFacade(
      new FakeCommentDataPort(),
      authService as unknown as AuthService,
      new FakeDestroyRef()
    );

    facade.initializeAuthorAccess();

    expect(facade.canWrite()).toBe(true);
  });

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
