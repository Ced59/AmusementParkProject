import { Signal, WritableSignal, signal } from '@angular/core';

export class FakeCommentRichTextImagesFacade {
  readonly uploadingSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly uploading: Signal<boolean> = this.uploadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  discardCount: number = 0;
  previewUrl: string | null = null;
  readonly committedImageIdSnapshots: string[][] = [];

  uploadImage(): Promise<{ id: string }> {
    return Promise.resolve({ id: '0123456789abcdef0123456789abcdef' });
  }

  discardDraftImages(): void {
    this.discardCount += 1;
  }

  markDraftImagesCommitted(imageIds: ReadonlySet<string>): void {
    this.committedImageIdSnapshots.push(Array.from(imageIds));
  }

  clearError(): void {
  }

  resolvePreviewUrl(): string | null {
    return this.previewUrl;
  }
}
