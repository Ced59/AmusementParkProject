import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT,
  AdminContextualBlockPreviewDataPort
} from './admin-contextual-block-preview-data.ports';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockPreviewFacade {
  private readonly jsonDraftSignal = signal<string>('');
  private readonly previewResultSignal = signal<ContextualBlockPreviewResult | null>(null);
  private readonly isPreviewingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;
  private previewRequestVersion: number = 0;

  public readonly jsonDraft: Signal<string> = this.jsonDraftSignal.asReadonly();
  public readonly previewResult: Signal<ContextualBlockPreviewResult | null> = this.previewResultSignal.asReadonly();
  public readonly isPreviewing: Signal<boolean> = this.isPreviewingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(@Inject(ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT) private readonly contextualBlocksApi: AdminContextualBlockPreviewDataPort) {
  }

  canPreview(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('boundedJsonPreview'));
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.invalidatePreviewRequest();
    this.jsonDraftSignal.set('');
    this.previewResultSignal.set(null);
    this.errorKeySignal.set(null);
  }

  setJsonDraft(value: string): void {
    this.invalidatePreviewRequest();
    this.jsonDraftSignal.set(value);
    this.previewResultSignal.set(null);
    this.errorKeySignal.set(null);
  }

  clearDraft(): void {
    this.invalidatePreviewRequest();
    this.jsonDraftSignal.set('');
    this.previewResultSignal.set(null);
    this.errorKeySignal.set(null);
  }

  useServerResult(result: ContextualBlockPreviewResult): void {
    this.invalidatePreviewRequest();
    this.previewResultSignal.set(result);
    this.errorKeySignal.set(null);
  }

  previewBlock(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.previewResultSignal.set(null);

    if (!this.canPreview(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.previewJsonUnavailable');
      return;
    }

    if (!block.entityId.trim()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.previewJsonError');
      return;
    }

    const draft: string = this.jsonDraftSignal().trim();
    if (!draft) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.previewJsonInvalid');
      return;
    }

    let parsedDocument: unknown;
    try {
      parsedDocument = JSON.parse(draft) as unknown;
    } catch {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.previewJsonInvalid');
      return;
    }

    this.isPreviewingSignal.set(true);
    const requestVersion: number = ++this.previewRequestVersion;
    const requestBlockId: string = block.id;
    this.contextualBlocksApi.previewBlock(block.type, block.entityId, parsedDocument)
      .pipe(finalize((): void => {
        if (this.isCurrentPreviewRequest(requestVersion, requestBlockId)) {
          this.isPreviewingSignal.set(false);
        }
      }))
      .subscribe({
        next: (result: ContextualBlockPreviewResult): void => {
          if (!this.isCurrentPreviewRequest(requestVersion, requestBlockId)) {
            return;
          }

          this.previewResultSignal.set(result);
        },
        error: (): void => {
          if (!this.isCurrentPreviewRequest(requestVersion, requestBlockId)) {
            return;
          }

          this.errorKeySignal.set('admin.contextualBlocks.drawer.previewJsonError');
        }
      });
  }

  private invalidatePreviewRequest(): void {
    this.previewRequestVersion++;
    this.isPreviewingSignal.set(false);
  }

  private isCurrentPreviewRequest(requestVersion: number, blockId: string): boolean {
    return this.previewRequestVersion === requestVersion && this.activeBlockId === blockId;
  }
}
