import { ChangeDetectionStrategy, Component, effect, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ContextualBlockPreviewChange, ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import {
  AdminContextualBlockCapability,
  AdminContextualBlockInstance
} from '../../models/admin-contextual-block.model';
import { AdminContextualBlockApplyFacade } from '../../state/admin-contextual-block-apply.facade';
import { AdminContextualBlockExportFacade } from '../../state/admin-contextual-block-export.facade';
import { AdminContextualBlockPreviewFacade } from '../../state/admin-contextual-block-preview.facade';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';

interface AdminContextualBlockIdEntry {
  readonly key: string;
  readonly value: string;
}

@Component({
  selector: 'app-admin-contextual-block-drawer',
  templateUrl: './admin-contextual-block-drawer.component.html',
  styleUrl: './admin-contextual-block-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class AdminContextualBlockDrawerComponent {
  protected readonly selectedBlock: Signal<AdminContextualBlockInstance | null> = this.selectionFacade.selectedBlock;
  protected readonly isExporting: Signal<boolean> = this.exportFacade.isExporting;
  protected readonly exportErrorKey: Signal<string | null> = this.exportFacade.errorKey;
  protected readonly jsonDraft: Signal<string> = this.previewFacade.jsonDraft;
  protected readonly previewResult: Signal<ContextualBlockPreviewResult | null> = this.previewFacade.previewResult;
  protected readonly isPreviewing: Signal<boolean> = this.previewFacade.isPreviewing;
  protected readonly previewErrorKey: Signal<string | null> = this.previewFacade.errorKey;
  protected readonly applyResult: Signal<ContextualBlockPreviewResult | null> = this.applyFacade.applyResult;
  protected readonly isApplying: Signal<boolean> = this.applyFacade.isApplying;
  protected readonly applyErrorKey: Signal<string | null> = this.applyFacade.errorKey;

  constructor(
    private readonly selectionFacade: AdminContextualBlockSelectionFacade,
    private readonly exportFacade: AdminContextualBlockExportFacade,
    private readonly previewFacade: AdminContextualBlockPreviewFacade,
    private readonly applyFacade: AdminContextualBlockApplyFacade
  ) {
    effect((): void => {
      const selectedBlock: AdminContextualBlockInstance | null = this.selectedBlock();
      this.previewFacade.resetForBlock(selectedBlock);
      this.applyFacade.resetForBlock(selectedBlock);
    });
  }

  protected close(): void {
    this.selectionFacade.clearSelection();
  }

  protected canDownloadJson(block: AdminContextualBlockInstance): boolean {
    return this.exportFacade.canExport(block);
  }

  protected downloadJson(block: AdminContextualBlockInstance): void {
    this.exportFacade.exportBlock(block);
  }

  protected canPreviewJson(block: AdminContextualBlockInstance): boolean {
    return this.previewFacade.canPreview(block);
  }

  protected canApplyJson(block: AdminContextualBlockInstance): boolean {
    return this.applyFacade.canApply(block);
  }

  protected canApplyCurrentPreview(block: AdminContextualBlockInstance): boolean {
    return this.applyFacade.hasAcceptedPreview(block);
  }

  protected updateJsonDraft(event: Event): void {
    const target: HTMLTextAreaElement | null = event.target instanceof HTMLTextAreaElement ? event.target : null;
    this.previewFacade.setJsonDraft(target?.value ?? '');
    this.applyFacade.clearResult();
  }

  protected previewJson(block: AdminContextualBlockInstance): void {
    this.applyFacade.clearResult();
    this.previewFacade.previewBlock(block);
  }

  protected applyJson(block: AdminContextualBlockInstance): void {
    this.applyFacade.applyBlock(block);
  }

  protected clearJsonDraft(): void {
    this.previewFacade.clearDraft();
    this.applyFacade.clearResult();
  }

  protected getIdEntries(block: AdminContextualBlockInstance): AdminContextualBlockIdEntry[] {
    return Object.entries(block.ids).map(([key, value]: [string, string]) => ({ key, value }));
  }

  protected getCapabilityLabelKey(capability: AdminContextualBlockCapability): string {
    return `admin.contextualBlocks.capabilities.${capability}`;
  }

  protected getPreviewStatusKey(result: ContextualBlockPreviewResult): string {
    if (result.isApplied) {
      return 'admin.contextualBlocks.drawer.applyJsonSucceeded';
    }

    return result.canApply
      ? 'admin.contextualBlocks.drawer.previewJsonCanApply'
      : 'admin.contextualBlocks.drawer.previewJsonBlocked';
  }

  protected hasPreviewValue(value: string | null): boolean {
    return value !== null && value.length > 0;
  }

  protected trackPreviewChange(change: ContextualBlockPreviewChange): string {
    return `${change.entityType}:${change.entityId}:${change.field}:${change.languageCode ?? ''}`;
  }
}
