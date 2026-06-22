import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, Signal, signal } from '@angular/core';

import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockParkGraphUpsertFacade {
  private readonly isCopyingSignal = signal<boolean>(false);
  private readonly isDownloadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;

  public readonly isCopying: Signal<boolean> = this.isCopyingSignal.asReadonly();
  public readonly isDownloading: Signal<boolean> = this.isDownloadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
  }

  canUseDraft(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('parkGraphUpsertDraft') && this.getDraft(block));
  }

  getDraft(block: AdminContextualBlockInstance | null): string | null {
    const draft: string = block?.parkGraphUpsertDraftJson?.trim() ?? '';
    return draft.length > 0 ? draft : null;
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.isCopyingSignal.set(false);
    this.isDownloadingSignal.set(false);
  }

  async copyDraft(block: AdminContextualBlockInstance): Promise<void> {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    const draft: string | null = this.getDraft(block);
    if (!this.canUseDraft(block) || !draft) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertUnavailable');
      return;
    }

    this.isCopyingSignal.set(true);
    try {
      await this.writeToClipboard(draft);
      this.successKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertCopied');
    } catch {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertCopyError');
    } finally {
      this.isCopyingSignal.set(false);
    }
  }

  downloadDraft(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    const draft: string | null = this.getDraft(block);
    if (!this.canUseDraft(block) || !draft) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertUnavailable');
      return;
    }

    this.isDownloadingSignal.set(true);
    try {
      this.downloadText(draft, this.resolveFileName(block));
      this.successKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertDownloaded');
    } catch {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.parkGraphUpsertDownloadError');
    } finally {
      this.isDownloadingSignal.set(false);
    }
  }

  private async writeToClipboard(value: string): Promise<void> {
    const defaultView: Window | null = this.document.defaultView;
    if (defaultView?.navigator.clipboard?.writeText) {
      await defaultView.navigator.clipboard.writeText(value);
      return;
    }

    const textArea: HTMLTextAreaElement = this.document.createElement('textarea');
    textArea.value = value;
    textArea.setAttribute('readonly', 'true');
    textArea.style.position = 'fixed';
    textArea.style.left = '-9999px';
    this.document.body.appendChild(textArea);
    textArea.select();
    this.document.execCommand('copy');
    this.document.body.removeChild(textArea);
  }

  private downloadText(value: string, fileName: string): void {
    const defaultView: Window | null = this.document.defaultView;
    if (!defaultView || typeof URL === 'undefined' || typeof URL.createObjectURL !== 'function') {
      throw new Error('Blob downloads are unavailable.');
    }

    const blob: Blob = new Blob([value], { type: 'application/json;charset=utf-8' });
    const objectUrl: string = URL.createObjectURL(blob);
    const anchor: HTMLAnchorElement = this.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    this.document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  private resolveFileName(block: AdminContextualBlockInstance): string {
    const fileName: string = block.parkGraphUpsertFileName?.trim() ?? '';
    return fileName.length > 0 ? fileName : `manufacturer-${block.entityId}-park-graph-upsert.json`;
  }
}
