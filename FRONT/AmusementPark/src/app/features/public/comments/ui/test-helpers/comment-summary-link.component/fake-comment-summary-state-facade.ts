import { signal, WritableSignal } from '@angular/core';

import { CommentSummary } from '@app/models/comments/comment.models';

export class FakeCommentSummaryStateFacade {
  readonly summary: WritableSignal<CommentSummary | null> = signal<CommentSummary | null>({
    targetType: 'Park',
    targetId: 'park-1',
    commentCount: 0,
    languageCode: 'fr',
    languageCommentCount: 0,
    officialComment: null,
  });
  readonly canWrite: WritableSignal<boolean> = signal<boolean>(true);
  readonly initializeAuthorAccess = vi.fn();
  readonly load = vi.fn();
}
