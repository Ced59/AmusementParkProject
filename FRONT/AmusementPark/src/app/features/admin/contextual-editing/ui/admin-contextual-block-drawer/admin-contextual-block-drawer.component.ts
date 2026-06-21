import { ChangeDetectionStrategy, Component, effect, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ContextualBlockPreviewChange, ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import {
  AdminContextualBlockCapability,
  AdminContextualBlockInstance
} from '../../models/admin-contextual-block.model';
import { AdminContextualBlockApplyFacade } from '../../state/admin-contextual-block-apply.facade';
import { AdminContextualBlockChildAddFacade, AdminContextualBlockChildAddZoneOption } from '../../state/admin-contextual-block-child-add.facade';
import { AdminContextualBlockExportFacade } from '../../state/admin-contextual-block-export.facade';
import { AdminContextualBlockFormFacade, AdminContextualBlockLocalizedFormField } from '../../state/admin-contextual-block-form.facade';
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
  protected readonly localizedFormFields: Signal<readonly AdminContextualBlockLocalizedFormField[]> = this.formFacade.localizedFields;
  protected readonly isFormLoading: Signal<boolean> = this.formFacade.isLoading;
  protected readonly isFormSaving: Signal<boolean> = this.formFacade.isSaving;
  protected readonly formErrorKey: Signal<string | null> = this.formFacade.errorKey;
  protected readonly formSuccessKey: Signal<string | null> = this.formFacade.successKey;
  protected readonly childAddName: Signal<string> = this.childAddFacade.itemName;
  protected readonly childAddSelectedZoneId: Signal<string | null> = this.childAddFacade.selectedZoneId;
  protected readonly childAddZoneOptions: Signal<readonly AdminContextualBlockChildAddZoneOption[]> = this.childAddFacade.zoneOptions;
  protected readonly isChildAddLoadingZones: Signal<boolean> = this.childAddFacade.isLoadingZones;
  protected readonly isChildAddCreating: Signal<boolean> = this.childAddFacade.isCreating;
  protected readonly childAddErrorKey: Signal<string | null> = this.childAddFacade.errorKey;
  protected readonly childAddSuccessKey: Signal<string | null> = this.childAddFacade.successKey;
  protected readonly createdChildAdminRoute: Signal<readonly string[] | null> = this.childAddFacade.createdItemAdminRoute;

  constructor(
    private readonly selectionFacade: AdminContextualBlockSelectionFacade,
    private readonly exportFacade: AdminContextualBlockExportFacade,
    private readonly previewFacade: AdminContextualBlockPreviewFacade,
    private readonly applyFacade: AdminContextualBlockApplyFacade,
    private readonly formFacade: AdminContextualBlockFormFacade,
    private readonly childAddFacade: AdminContextualBlockChildAddFacade
  ) {
    effect((): void => {
      const selectedBlock: AdminContextualBlockInstance | null = this.selectedBlock();
      this.previewFacade.resetForBlock(selectedBlock);
      this.applyFacade.resetForBlock(selectedBlock);
      this.formFacade.resetForBlock(selectedBlock);
      this.childAddFacade.resetForBlock(selectedBlock);
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

  protected canEditForm(block: AdminContextualBlockInstance): boolean {
    return this.formFacade.canEditForm(block);
  }

  protected canAddChild(block: AdminContextualBlockInstance): boolean {
    return this.childAddFacade.canAddChild(block);
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

  protected reloadForm(block: AdminContextualBlockInstance): void {
    this.formFacade.loadForm(block);
  }

  protected updateLocalizedFormField(languageCode: string, event: Event): void {
    const target: HTMLTextAreaElement | null = event.target instanceof HTMLTextAreaElement ? event.target : null;
    this.formFacade.updateLocalizedValue(languageCode, target?.value ?? '');
  }

  protected saveForm(block: AdminContextualBlockInstance): void {
    this.formFacade.saveForm(block);
  }

  protected updateChildAddName(event: Event): void {
    const target: HTMLInputElement | null = event.target instanceof HTMLInputElement ? event.target : null;
    this.childAddFacade.updateItemName(target?.value ?? '');
  }

  protected updateChildAddZone(event: Event): void {
    const target: HTMLSelectElement | null = event.target instanceof HTMLSelectElement ? event.target : null;
    this.childAddFacade.updateSelectedZoneId(target?.value ?? null);
  }

  protected createChild(block: AdminContextualBlockInstance): void {
    this.childAddFacade.createChild(block);
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

  protected trackLocalizedFormField(field: AdminContextualBlockLocalizedFormField): string {
    return field.languageCode;
  }

  protected trackChildAddZone(zone: AdminContextualBlockChildAddZoneOption): string {
    return zone.id;
  }
}
