import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import {
  ContextualBlockExportDocument,
  ContextualBlockLocalizedText,
  ContextualParkDescriptionBlock
} from '@shared/models/admin/contextual-block-export.models';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT,
  AdminContextualBlockFormDataPort
} from './admin-contextual-block-form-data.ports';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

export interface AdminContextualBlockLocalizedFormField {
  readonly languageCode: string;
  readonly value: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockFormFacade {
  private readonly localizedFieldsSignal = signal<readonly AdminContextualBlockLocalizedFormField[]>([]);
  private readonly isLoadingSignal = signal<boolean>(false);
  private readonly isSavingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;
  private currentDocument: ContextualBlockExportDocument<ContextualParkDescriptionBlock> | null = null;

  public readonly localizedFields: Signal<readonly AdminContextualBlockLocalizedFormField[]> = this.localizedFieldsSignal.asReadonly();
  public readonly isLoading: Signal<boolean> = this.isLoadingSignal.asReadonly();
  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT) private readonly contextualBlocksApi: AdminContextualBlockFormDataPort,
    private readonly refreshEvents: AdminContextualBlockRefreshEvents
  ) {
  }

  canEditForm(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('contextualFormEdit'));
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.currentDocument = null;
    this.localizedFieldsSignal.set([]);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.isLoadingSignal.set(false);
    this.isSavingSignal.set(false);

    if (block !== null && this.canEditForm(block)) {
      this.loadForm(block);
    }
  }

  loadForm(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    if (!this.canEditForm(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formUnavailable');
      return;
    }

    if (!block.entityId.trim()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
      return;
    }

    this.isLoadingSignal.set(true);
    this.contextualBlocksApi.getBlockExportDocument<ContextualParkDescriptionBlock>(block.type, block.entityId)
      .pipe(finalize((): void => this.isLoadingSignal.set(false)))
      .subscribe({
        next: (document: ContextualBlockExportDocument<ContextualParkDescriptionBlock>): void => {
          if (!this.isParkDescriptionDocument(document, block)) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
            return;
          }

          this.currentDocument = document;
          this.localizedFieldsSignal.set(document.block.descriptions.map((description: ContextualBlockLocalizedText) => ({
            languageCode: description.languageCode,
            value: description.value ?? ''
          })));
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formLoadError');
        }
      });
  }

  updateLocalizedValue(languageCode: string, value: string): void {
    this.localizedFieldsSignal.update((fields: readonly AdminContextualBlockLocalizedFormField[]) => fields.map((field: AdminContextualBlockLocalizedFormField) => {
      if (field.languageCode !== languageCode) {
        return field;
      }

      return {
        ...field,
        value,
      };
    }));
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  saveForm(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    const currentDocument: ContextualBlockExportDocument<ContextualParkDescriptionBlock> | null = this.currentDocument;

    if (!this.canEditForm(block) || !currentDocument?.block) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
      return;
    }

    const document: ContextualBlockExportDocument<ContextualParkDescriptionBlock> = {
      ...currentDocument,
      block: {
        ...currentDocument.block,
        descriptions: this.localizedFieldsSignal().map((field: AdminContextualBlockLocalizedFormField) => ({
          languageCode: field.languageCode,
          value: field.value.length === 0 ? null : field.value
        }))
      }
    };

    this.isSavingSignal.set(true);
    this.contextualBlocksApi.applyBlock(block.type, block.entityId, document)
      .pipe(finalize((): void => this.isSavingSignal.set(false)))
      .subscribe({
        next: (result: ContextualBlockPreviewResult): void => {
          if (!result.canApply) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
            return;
          }

          if (!result.isApplied) {
            this.errorKeySignal.set('admin.contextualBlocks.drawer.formNoChanges');
            return;
          }

          this.currentDocument = document;
          this.successKeySignal.set('admin.contextualBlocks.drawer.formSaveSucceeded');
          this.refreshEvents.notifyBlockApplied({
            blockType: block.type,
            entityType: block.entityType,
            entityId: result.target.entityId,
            appliedAtUtc: new Date().toISOString()
          });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.formSaveError');
        }
      });
  }

  private isParkDescriptionDocument(
    document: ContextualBlockExportDocument<ContextualParkDescriptionBlock>,
    block: AdminContextualBlockInstance
  ): document is ContextualBlockExportDocument<ContextualParkDescriptionBlock> & { block: ContextualParkDescriptionBlock } {
    return document.blockType === block.type &&
      document.target.entityType === block.entityType &&
      document.target.entityId === block.entityId &&
      document.block !== null &&
      document.block.parkId === block.entityId &&
      Array.isArray(document.block.descriptions);
  }
}
