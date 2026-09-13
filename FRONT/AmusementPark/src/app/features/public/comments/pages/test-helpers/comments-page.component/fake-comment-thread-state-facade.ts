import { Signal, WritableSignal, signal } from '@angular/core';

import { CommentThread, CreateCommentRequest, UpdateCommentRequest } from '@app/models/comments/comment.models';

import { ScreenState } from '@shared/models/contracts/screen-state.model';

import { CommentEditorResetReason } from '../../../state/comment-thread-state.facade';

export class FakeCommentThreadStateFacade {
  readonly stateSignal: WritableSignal<ScreenState<CommentThread, string>> =
    signal<ScreenState<CommentThread, string>>({ kind: 'loading' });
  readonly state: Signal<ScreenState<CommentThread, string>> = this.stateSignal.asReadonly();
  readonly threadSignal: WritableSignal<CommentThread | null> = signal<CommentThread | null>(null);
  readonly thread: Signal<CommentThread | null> = this.threadSignal.asReadonly();
  readonly canWriteSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly canWrite: Signal<boolean> = this.canWriteSignal.asReadonly();
  readonly canManageSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly canManage: Signal<boolean> = this.canManageSignal.asReadonly();
  readonly savingSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly saveErrorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  readonly editorResetVersionSignal: WritableSignal<number> = signal<number>(0);
  readonly editorResetVersion: Signal<number> = this.editorResetVersionSignal.asReadonly();
  readonly editorResetReasonSignal: WritableSignal<CommentEditorResetReason | null> =
    signal<CommentEditorResetReason | null>(null);
  readonly editorResetReason: Signal<CommentEditorResetReason | null> =
    this.editorResetReasonSignal.asReadonly();
  readonly notFoundSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  readonly deleteCalls: Array<{ commentId: string; revision: number }> = [];
  readonly createCalls: CreateCommentRequest[] = [];
  readonly updateCalls: UpdateCommentRequest[] = [];

  initializeAuthorAccess(): void {
  }

  load(_targetType: 'Park' | 'ParkItem', _targetId: string): void {
  }

  create(request: CreateCommentRequest): void {
    this.createCalls.push(request);
  }

  update(request: UpdateCommentRequest): void {
    this.updateCalls.push(request);
  }

  delete(commentId: string, revision: number): void {
    this.deleteCalls.push({ commentId, revision });
  }

  clearSaveError(): void {
  }
}
