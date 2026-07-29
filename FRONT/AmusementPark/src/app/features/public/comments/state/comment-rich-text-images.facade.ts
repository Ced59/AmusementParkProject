import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';

import { CommentImageUpload, ManagedRichTextImage } from '@app/models/comments/comment-image.models';
import { COMMENT_DATA_PORT, CommentDataPort } from './comment-data.ports';
import { normalizeManagedCommentImageId } from '@shared/utils/comments/managed-comment-image.helpers';

class DiscardedCommentImageUploadError extends Error {
  constructor() {
    super('The comment image draft was discarded.');
  }
}

@Injectable()
export class CommentRichTextImagesFacade {
  private readonly uploadingCountSignal = signal<number>(0);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly draftImageIds: Set<string> = new Set<string>();
  private readonly previewUrls: Map<string, string> = new Map<string, string>();
  private uploadQueue: Promise<void> = Promise.resolve();
  private draftSession: number = 0;

  readonly uploading: Signal<boolean> = computed(() => this.uploadingCountSignal() > 0);
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(COMMENT_DATA_PORT) private readonly commentDataPort: CommentDataPort,
    private readonly destroyRef: DestroyRef
  ) {
    this.destroyRef.onDestroy((): void => {
      this.discardDraftImages();
    });
  }

  uploadImage(file: File): Promise<ManagedRichTextImage> {
    const session: number = this.draftSession;
    this.uploadingCountSignal.update((count: number) => count + 1);
    this.errorKeySignal.set(null);

    const uploadTask: Promise<CommentImageUpload> = this.uploadQueue.then(
      (): Promise<CommentImageUpload> => firstValueFrom(
        this.commentDataPort.uploadCommentImage(file).pipe(takeUntilDestroyed(this.destroyRef))
      )
    );
    this.uploadQueue = uploadTask.then((): void => undefined, (): void => undefined);

    return uploadTask.then((uploaded: CommentImageUpload): ManagedRichTextImage => {
      const imageId: string | null = normalizeManagedCommentImageId(uploaded.id);
      if (imageId === null) {
        throw new Error('The comment image upload response does not contain a valid id.');
      }

      if (session !== this.draftSession) {
        this.deleteImageWithoutBlocking(imageId);
        throw new DiscardedCommentImageUploadError();
      }

      this.draftImageIds.add(imageId);
      const previewUrl: string | undefined = this.createPreviewUrl(file);
      if (previewUrl) {
        this.previewUrls.set(imageId, previewUrl);
      }
      return { id: imageId, previewUrl };
    }).catch((error: unknown): never => {
      if (!(error instanceof DiscardedCommentImageUploadError)) {
        this.errorKeySignal.set('comments.editor.images.uploadError');
      }

      throw error;
    }).finally((): void => {
      this.uploadingCountSignal.update((count: number) => Math.max(0, count - 1));
    });
  }

  deleteDraftImage(imageId: string): void {
    const normalizedImageId: string = imageId.trim();
    if (!this.draftImageIds.delete(normalizedImageId)) {
      return;
    }

    this.revokePreviewUrl(normalizedImageId);
    this.deleteImageWithoutBlocking(normalizedImageId);
  }

  discardDraftImages(): void {
    this.draftSession += 1;
    const imageIds: string[] = Array.from(this.draftImageIds);
    this.draftImageIds.clear();
    for (const imageId of imageIds) {
      this.revokePreviewUrl(imageId);
      this.deleteImageWithoutBlocking(imageId);
    }
  }

  markDraftImagesCommitted(): void {
    this.draftSession += 1;
    this.draftImageIds.clear();
    this.revokeAllPreviewUrls();
    this.errorKeySignal.set(null);
  }

  clearError(): void {
    this.errorKeySignal.set(null);
  }

  resolvePreviewUrl(imageId: string): string | null {
    return this.previewUrls.get(imageId) ?? null;
  }

  private deleteImageWithoutBlocking(imageId: string): void {
    this.commentDataPort.deleteCommentImage(imageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: (): void => {
          this.errorKeySignal.set('comments.editor.images.cleanupError');
        }
      });
  }

  private createPreviewUrl(file: File): string | undefined {
    if (typeof URL.createObjectURL !== 'function') {
      return undefined;
    }

    try {
      return URL.createObjectURL(file);
    } catch {
      return undefined;
    }
  }

  private revokePreviewUrl(imageId: string): void {
    const previewUrl: string | undefined = this.previewUrls.get(imageId);
    if (!previewUrl) {
      return;
    }

    this.previewUrls.delete(imageId);
    if (typeof URL.revokeObjectURL === 'function') {
      URL.revokeObjectURL(previewUrl);
    }
  }

  private revokeAllPreviewUrls(): void {
    for (const imageId of Array.from(this.previewUrls.keys())) {
      this.revokePreviewUrl(imageId);
    }
  }
}
