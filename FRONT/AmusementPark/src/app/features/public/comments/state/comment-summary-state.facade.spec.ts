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
