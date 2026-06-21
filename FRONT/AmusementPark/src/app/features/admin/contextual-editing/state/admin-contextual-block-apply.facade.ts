import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT,
  AdminContextualBlockApplyDataPort
} from './admin-contextual-block-apply-data.ports';
import { AdminContextualBlockPreviewFacade } from './admin-contextual-block-preview.facade';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockApplyFacade {
  private readonly applyResultSignal = signal<ContextualBlockPreviewResult | null>(null);
  private readonly isApplyingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;

  public readonly applyResult: Signal<ContextualBlockPreviewResult | null> = this.applyResultSignal.asReadonly();
  public readonly isApplying: Signal<boolean> = this.isApplyingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT) private readonly contextualBlocksApi: AdminContextualBlockApplyDataPort,
    private readonly previewFacade: AdminContextualBlockPreviewFacade,
    private readonly refreshEvents: AdminContextualBlockRefreshEvents
  ) {
  }

  canApply(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('boundedJsonApply'));
  }

  hasAcceptedPreview(block: AdminContextualBlockInstance): boolean {
    const previewResult: ContextualBlockPreviewResult | null = this.previewFacade.previewResult();
    return Boolean(
      previewResult?.canApply &&
      previewResult.blockType === block.type &&
      previewResult.target.entityType === block.entityType &&
      previewResult.target.entityId === block.entityId
    );
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.applyResultSignal.set(null);
    this.errorKeySignal.set(null);
    this.isApplyingSignal.set(false);
  }

  clearResult(): void {
    this.applyResultSignal.set(null);
    this.errorKeySignal.set(null);
  }

  applyBlock(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.applyResultSignal.set(null);

    if (!this.canApply(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonUnavailable');
      return;
    }

    if (!this.hasAcceptedPreview(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonPreviewRequired');
      return;
    }

    if (!block.entityId.trim()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonError');
      return;
    }

    const draft: string = this.previewFacade.jsonDraft().trim();
    if (!draft) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonInvalid');
      return;
    }

    let parsedDocument: unknown;
    try {
      parsedDocument = JSON.parse(draft) as unknown;
    } catch {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonInvalid');
      return;
    }

    this.isApplyingSignal.set(true);
    this.contextualBlocksApi.applyBlock(block.type, block.entityId, parsedDocument)
      .pipe(finalize((): void => this.isApplyingSignal.set(false)))
      .subscribe({
        next: (result: ContextualBlockPreviewResult): void => {
          this.applyResultSignal.set(result);
          this.previewFacade.useServerResult(result);

          if (!result.isApplied || !result.canApply) {
            return;
          }

          if (result.target.entityType !== block.entityType) {
            return;
          }

          this.refreshEvents.notifyBlockApplied({
            blockType: block.type,
            entityType: block.entityType,
            entityId: result.target.entityId,
            appliedAtUtc: new Date().toISOString()
          });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.applyJsonError');
        }
      });
  }
}
