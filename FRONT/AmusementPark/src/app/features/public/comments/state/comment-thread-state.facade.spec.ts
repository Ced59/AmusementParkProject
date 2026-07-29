import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment
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
  threadResponse: Observable<CommentThread> | null = null;
  readonly createCalls: CreateCommentRequest[] = [];

  getSummary(targetType: CommentTargetType, targetId: string): Observable<CommentSummary> {
    return of({
      targetType,
      targetId,
      commentCount: this.thread.comments.length,
      officialComment: this.thread.comments.find((comment: PublicComment) => comment.isOfficial) ?? null
    });
  }

  getThread(_targetType: CommentTargetType, _targetId: string): Observable<CommentThread> {
    return this.threadResponse ?? of(this.thread);
  }

  createComment(request: CreateCommentRequest): Observable<PublicComment> {
    this.createCalls.push(request);
    return of(this.createdComment);
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
    expect(context.facade.createdVersion()).toBe(1);
    expect(context.toastMessageService.messages).toEqual(['comments.editor.saved']);
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
    expect(context.dataPort.createCalls).toEqual([]);
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

function createComment(id: string, isOfficial: boolean, createdAtUtc: string): PublicComment {
  return {
    id,
    targetType: 'Park',
    targetId: 'park-1',
    authorDisplayName: 'Alice',
    authorRole: 'Admin',
    bodies: [{ languageCode: 'fr', value: `<p>${id}</p>` }],
    isOfficial,
    createdAtUtc,
    updatedAtUtc: createdAtUtc
  };
}
