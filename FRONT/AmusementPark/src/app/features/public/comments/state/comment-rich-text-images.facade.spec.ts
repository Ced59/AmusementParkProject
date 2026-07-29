import { DestroyRef } from '@angular/core';
import { Observable, Subject, of } from 'rxjs';

import { CommentImageUpload } from '@app/models/comments/comment-image.models';
import {
  CommentSummary,
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { CommentDataPort } from './comment-data.ports';
import { CommentRichTextImagesFacade } from './comment-rich-text-images.facade';

class FakeDestroyRef implements DestroyRef {
  readonly destroyed: boolean = false;
  private callback: (() => void) | null = null;

  onDestroy(callback: () => void): () => void {
    this.callback = callback;
    return (): void => {
      this.callback = null;
    };
  }

  destroy(): void {
    this.callback?.();
  }
}

class FakeCommentDataPort implements CommentDataPort {
  readonly uploadSubjects: Subject<CommentImageUpload>[] = [];
  readonly uploadedFiles: File[] = [];
  readonly deletedImageIds: string[] = [];

  getSummary(_targetType: CommentTargetType, _targetId: string): Observable<CommentSummary> {
    throw new Error('Not used.');
  }

  getThread(_targetType: CommentTargetType, _targetId: string): Observable<CommentThread> {
    throw new Error('Not used.');
  }

  createComment(_request: CreateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  updateComment(_request: UpdateCommentRequest): Observable<PublicComment> {
    throw new Error('Not used.');
  }

  deleteComment(_commentId: string): Observable<void> {
    throw new Error('Not used.');
  }

  uploadCommentImage(file: File): Observable<CommentImageUpload> {
    this.uploadedFiles.push(file);
    const subject: Subject<CommentImageUpload> = new Subject<CommentImageUpload>();
    this.uploadSubjects.push(subject);
    return subject.asObservable();
  }

  deleteCommentImage(imageId: string): Observable<void> {
    this.deletedImageIds.push(imageId);
    return of(undefined);
  }
}

describe('CommentRichTextImagesFacade', () => {
  const firstImageId: string = '0123456789abcdef0123456789abcdef';
  const secondImageId: string = 'abcdef0123456789abcdef0123456789';
  const lateImageId: string = '11111111111111111111111111111111';

  it('uploads files sequentially and tracks the in-progress state', async () => {
    const port: FakeCommentDataPort = new FakeCommentDataPort();
    const facade: CommentRichTextImagesFacade = new CommentRichTextImagesFacade(
      port,
      new FakeDestroyRef()
    );
    const firstFile: File = new File(['first'], 'first.png', { type: 'image/png' });
    const secondFile: File = new File(['second'], 'second.png', { type: 'image/png' });

    const firstUpload: Promise<{ id: string }> = facade.uploadImage(firstFile);
    const secondUpload: Promise<{ id: string }> = facade.uploadImage(secondFile);
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(1));

    expect(facade.uploading()).toBe(true);
    port.uploadSubjects[0].next({ id: firstImageId, url: `/images/${firstImageId}` });
    port.uploadSubjects[0].complete();
    await expect(firstUpload).resolves.toEqual(expect.objectContaining({ id: firstImageId }));
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(2));

    port.uploadSubjects[1].next({ id: secondImageId, url: `/images/${secondImageId}` });
    port.uploadSubjects[1].complete();
    await expect(secondUpload).resolves.toEqual(expect.objectContaining({ id: secondImageId }));
    expect(port.uploadedFiles).toEqual([firstFile, secondFile]);
    expect(facade.uploading()).toBe(false);
  });

  it('deletes only uncommitted images when a draft is changed or abandoned', async () => {
    const port: FakeCommentDataPort = new FakeCommentDataPort();
    const facade: CommentRichTextImagesFacade = new CommentRichTextImagesFacade(
      port,
      new FakeDestroyRef()
    );
    const firstUpload: Promise<{ id: string }> = facade.uploadImage(
      new File(['first'], 'first.png', { type: 'image/png' })
    );
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(1));
    port.uploadSubjects[0].next({ id: firstImageId, url: `/images/${firstImageId}` });
    port.uploadSubjects[0].complete();
    await firstUpload;

    facade.deleteDraftImage(firstImageId);
    expect(port.deletedImageIds).toEqual([firstImageId]);

    const secondUpload: Promise<{ id: string }> = facade.uploadImage(
      new File(['second'], 'second.png', { type: 'image/png' })
    );
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(2));
    port.uploadSubjects[1].next({ id: secondImageId, url: `/images/${secondImageId}` });
    port.uploadSubjects[1].complete();
    await secondUpload;
    facade.markDraftImagesCommitted();
    facade.discardDraftImages();

    expect(port.deletedImageIds).toEqual([firstImageId]);
  });

  it('deletes an upload that finishes after its draft was cancelled', async () => {
    const port: FakeCommentDataPort = new FakeCommentDataPort();
    const facade: CommentRichTextImagesFacade = new CommentRichTextImagesFacade(
      port,
      new FakeDestroyRef()
    );
    const upload: Promise<{ id: string }> = facade.uploadImage(
      new File(['image'], 'late.png', { type: 'image/png' })
    );
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(1));

    facade.discardDraftImages();
    port.uploadSubjects[0].next({ id: lateImageId, url: `/images/${lateImageId}` });
    port.uploadSubjects[0].complete();

    await expect(upload).rejects.toThrow('discarded');
    expect(port.deletedImageIds).toEqual([lateImageId]);
  });

  it('uses a local object URL for private draft preview and revokes it after commit', async () => {
    const createObjectUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:draft-preview');
    const revokeObjectUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation((): void => {});
    const port: FakeCommentDataPort = new FakeCommentDataPort();
    const facade: CommentRichTextImagesFacade = new CommentRichTextImagesFacade(
      port,
      new FakeDestroyRef()
    );
    const file: File = new File(['image'], 'preview.png', { type: 'image/png' });
    const upload: Promise<{ id: string; previewUrl?: string }> = facade.uploadImage(file);
    await vi.waitFor((): void => expect(port.uploadSubjects).toHaveLength(1));
    port.uploadSubjects[0].next({ id: firstImageId, url: `/images/${firstImageId}` });
    port.uploadSubjects[0].complete();

    await expect(upload).resolves.toEqual({
      id: firstImageId,
      previewUrl: 'blob:draft-preview'
    });
    expect(facade.resolvePreviewUrl(firstImageId)).toBe('blob:draft-preview');
    expect(createObjectUrl).toHaveBeenCalledWith(file);

    facade.markDraftImagesCommitted();
    expect(revokeObjectUrl).toHaveBeenCalledWith('blob:draft-preview');
    expect(facade.resolvePreviewUrl(firstImageId)).toBeNull();
  });
});
