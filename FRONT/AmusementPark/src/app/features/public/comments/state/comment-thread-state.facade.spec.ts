import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { CommentDataPort } from './comment-data.ports';
import { CommentThreadStateFacade } from './comment-thread-state.facade';

class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}

class FakeCommentDataPort implements CommentDataPort {
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

class FakeAuthService {
  token: string | null = null;
  roles: string[] = [];

  ensureValidAccessToken(_forceRefreshAttempt: boolean): Observable<string | null> {
    return of(this.token);
  }

  hasRole(expectedRole: string): boolean {
    return this.roles.includes(expectedRole);
  }
}

class FakeToastMessageService {
  readonly messages: string[] = [];

  add(
    _severity: 'success' | 'info' | 'warn' | 'error',
    _summary: string,
    detail: string
  ): void {
    this.messages.push(detail);
  }
}

class FakeTranslateService {
  instant(key: string): string {
    return key;
  }
}

class FakeSsrHttpStatusService {
  readonly statuses: number[] = [];
  notFoundCallCount: number = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }

  setStatus(status: number): void {
    this.statuses.push(status);
  }
}

describe('CommentThreadStateFacade', () => {
  it('sorts the official review before newer regular comments', () => {
    const context = createFacade();

    context.facade.load('Park', ' park-1 ');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.thread()?.comments.map((comment: PublicComment) => comment.id)).toEqual([
      'official',
      'regular'
    ]);
  });

  it('allows a moderator to publish and keeps a new official review first', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['MODERATOR'];
    context.dataPort.createdComment = createComment('new-official', true, '2026-07-03T10:00:00Z');
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');
    const request: CreateCommentRequest = {
      targetType: 'Park',
      targetId: 'park-1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: true
    };

    context.facade.create(request);

    expect(context.facade.canWrite()).toBe(true);
    expect(context.dataPort.createCalls).toEqual([request]);
    expect(context.facade.thread()?.comments[0]?.id).toBe('new-official');
    expect(context.facade.editorResetVersion()).toBe(1);
    expect(context.facade.editorResetReason()).toBe('saved');
    expect(context.toastMessageService.messages).toEqual(['comments.editor.saved']);
  });

  it('allows an administrator to update an existing comment', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['ADMIN'];
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');
    const request: UpdateCommentRequest = {
      id: 'regular',
      bodies: [{ languageCode: 'fr', value: '<p>Corrigé</p>' }],
      isOfficial: true,
      revision: 1
    };

    context.facade.update(request);

    expect(context.facade.canManage()).toBe(true);
    expect(context.dataPort.updateCalls).toEqual([request]);
    expect(context.facade.thread()?.comments[0]?.id).toBe('regular');
    expect(context.facade.thread()?.comments[0]?.isOfficial).toBe(true);
    expect(context.facade.editorResetVersion()).toBe(1);
    expect(context.facade.editorResetReason()).toBe('saved');
    expect(context.toastMessageService.messages).toEqual(['comments.management.updated']);
  });

  it('allows an administrator to delete an existing comment', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['ADMIN'];
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');

    context.facade.delete('regular', 1);

    expect(context.dataPort.deleteCalls).toEqual([{ commentId: 'regular', revision: 1 }]);
    expect(context.facade.thread()?.comments.map((comment: PublicComment) => comment.id))
      .toEqual(['official']);
    expect(context.facade.editorResetVersion()).toBe(1);
    expect(context.facade.editorResetReason()).toBe('deleted');
    expect(context.toastMessageService.messages).toEqual(['comments.management.deleted']);
  });

  it('allows a moderator to manage their own comment', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['MODERATOR'];
    context.dataPort.thread = createThread([
      createComment('own', false, '2026-07-02T10:00:00Z', true)
    ]);
    context.dataPort.updatedComment = createComment(
      'own',
      false,
      '2026-07-03T10:00:00Z',
      true
    );
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');

    context.facade.update({
      id: 'own',
      bodies: [{ languageCode: 'fr', value: '<p>Corrigé</p>' }],
      isOfficial: false,
      revision: 1
    });
    context.facade.delete('own', 1);

    expect(context.facade.canWrite()).toBe(true);
    expect(context.facade.canManage()).toBe(true);
    expect(context.dataPort.updateCalls).toHaveLength(1);
    expect(context.dataPort.deleteCalls).toEqual([{ commentId: 'own', revision: 1 }]);
  });

  it('does not let a moderator manage another author comment', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['MODERATOR'];
    context.dataPort.thread = createThread([
      createComment('other', false, '2026-07-02T10:00:00Z', false)
    ]);
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');

    context.facade.update({
      id: 'other',
      bodies: [{ languageCode: 'fr', value: '<p>Interdit</p>' }],
      isOfficial: false,
      revision: 1
    });
    context.facade.delete('other', 1);

    expect(context.facade.canManage()).toBe(true);
    expect(context.dataPort.updateCalls).toEqual([]);
    expect(context.dataPort.deleteCalls).toEqual([]);
  });

  it('does not expose publication to a regular user', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['USER'];
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');

    context.facade.create({
      targetType: 'Park',
      targetId: 'park-1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: false
    });

    expect(context.facade.canWrite()).toBe(false);
    expect(context.facade.canManage()).toBe(true);
    expect(context.dataPort.createCalls).toEqual([]);
  });

  it('allows a regular user to manage only a comment marked as their own', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['USER'];
    context.dataPort.thread = createThread([
      createComment('own', false, '2026-07-02T10:00:00Z', true),
      createComment('other', false, '2026-07-01T10:00:00Z', false)
    ]);
    context.dataPort.updatedComment = createComment(
      'own',
      false,
      '2026-07-03T10:00:00Z',
      true
    );
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');

    context.facade.update({
      id: 'own',
      bodies: [{ languageCode: 'fr', value: '<p>Corrigé</p>' }],
      isOfficial: false,
      revision: 1
    });
    context.facade.delete('other', 1);

    expect(context.dataPort.updateCalls).toHaveLength(1);
    expect(context.dataPort.deleteCalls).toEqual([]);
  });

  it('reloads the latest revision after an update conflict without resetting the editor', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['USER'];
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');
    context.dataPort.thread = createThread([
      {
        ...createComment('regular', false, '2026-07-05T10:00:00Z'),
        bodies: [{ languageCode: 'fr', value: '<p>Version distante</p>' }],
        revision: 2
      }
    ]);
    context.dataPort.updateResponse = throwError(
      () => new HttpErrorResponse({ status: 409 })
    );

    context.facade.update({
      id: 'regular',
      bodies: [{ languageCode: 'fr', value: '<p>Mon brouillon</p>' }],
      isOfficial: false,
      revision: 1
    });

    expect(context.dataPort.getThreadCalls).toEqual([
      { targetType: 'Park', targetId: 'park-1' },
      { targetType: 'Park', targetId: 'park-1' }
    ]);
    expect(context.facade.thread()?.comments[0]?.revision).toBe(2);
    expect(context.facade.thread()?.comments[0]?.bodies[0]?.value)
      .toBe('<p>Version distante</p>');
    expect(context.facade.saveErrorKey()).toBe('comments.errors.concurrentModification');
    expect(context.facade.editorResetVersion()).toBe(0);
    expect(context.toastMessageService.messages)
      .toEqual(['comments.errors.concurrentModification']);
  });

  it('reloads the thread after a delete conflict', () => {
    const context = createFacade();
    context.authService.token = 'token';
    context.authService.roles = ['USER'];
    context.facade.initializeAuthorAccess();
    context.facade.load('Park', 'park-1');
    context.dataPort.thread = createThread([
      {
        ...createComment('regular', false, '2026-07-05T10:00:00Z'),
        revision: 3
      }
    ]);
    context.dataPort.deleteResponse = throwError(
      () => new HttpErrorResponse({ status: 409 })
    );

    context.facade.delete('regular', 1);

    expect(context.dataPort.getThreadCalls).toHaveLength(2);
    expect(context.facade.thread()?.comments[0]?.revision).toBe(3);
    expect(context.facade.saveErrorKey()).toBe('comments.errors.concurrentModification');
    expect(context.facade.editorResetVersion()).toBe(0);
  });

  it('marks a missing comment target as not found during SSR', () => {
    const context = createFacade();
    context.dataPort.threadResponse = throwError(
      () => new HttpErrorResponse({ status: 404 })
    );

    context.facade.load('Park', 'missing-park');

    expect(context.facade.state().kind).toBe('error');
    expect(context.facade.notFound()).toBe(true);
    expect(context.ssrHttpStatusService.notFoundCallCount).toBe(1);
    expect(context.ssrHttpStatusService.statuses).toEqual([]);
  });

  it('marks transient comment load failures as unavailable during SSR', () => {
    const context = createFacade();
    context.dataPort.threadResponse = throwError(
      () => new HttpErrorResponse({ status: 503 })
    );

    context.facade.load('Park', 'park-1');

    expect(context.facade.state().kind).toBe('error');
    expect(context.facade.notFound()).toBe(false);
    expect(context.ssrHttpStatusService.notFoundCallCount).toBe(0);
    expect(context.ssrHttpStatusService.statuses).toEqual([503]);
  });

  it('shows a load error when the API response does not match the requested target', () => {
    const context = createFacade();
    context.dataPort.threadResponse = of(createThread([]));

    context.facade.load('ParkItem', 'item-1');

    expect(context.facade.state()).toEqual({
      kind: 'error',
      error: 'comments.errors.load'
    });
  });
});

function createFacade(): {
  facade: CommentThreadStateFacade;
  dataPort: FakeCommentDataPort;
  authService: FakeAuthService;
  toastMessageService: FakeToastMessageService;
  ssrHttpStatusService: FakeSsrHttpStatusService;
} {
  const dataPort: FakeCommentDataPort = new FakeCommentDataPort();
  const authService: FakeAuthService = new FakeAuthService();
  const toastMessageService: FakeToastMessageService = new FakeToastMessageService();
  const ssrHttpStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();
  return {
    facade: new CommentThreadStateFacade(
      dataPort,
      authService as unknown as AuthService,
      toastMessageService as unknown as ToastMessageService,
      new FakeTranslateService() as unknown as TranslateService,
      new FakeDestroyRef(),
      ssrHttpStatusService as unknown as SsrHttpStatusService
    ),
    dataPort,
    authService,
    toastMessageService,
    ssrHttpStatusService
  };
}

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
