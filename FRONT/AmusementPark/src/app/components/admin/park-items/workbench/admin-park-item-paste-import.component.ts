import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  Signal,
  computed,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/ui/primitives/button';

import {
  ParkItemBulkCreateDraft,
  ParkItemBulkCreatePreviewRow,
  ParkItemsBulkCreateApplyResult,
  ParkItemsBulkCreatePreviewResult,
} from '@app/models/parks/park-item-bulk-create';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { parseParkItemPasteImport } from '@features/admin/park-items/import/parsers/park-item-paste-import.parser';
import {
  PARK_ITEM_CATEGORY_OPTIONS,
  PARK_ITEM_TYPE_OPTIONS,
  TranslationOption,
} from '@shared/utils/display/display-options';
import { resolveLocalizedValue } from '@shared/utils/localization';

@Component({
  selector: 'app-admin-park-item-paste-import',
  templateUrl: './admin-park-item-paste-import.component.html',
  styleUrls: ['./admin-park-item-paste-import.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonDirective, FormsModule, TranslateModule],
})
export class AdminParkItemPasteImportComponent {
  @Input({ required: true }) parkId!: string;
  @Input({ required: true }) zones!: Signal<ParkZone[]>;
  @Input({ required: true }) manufacturerOptions!: Signal<EntitySelectOption[]>;
  @Input({ required: true }) previewHandler!: (rows: ParkItemBulkCreateDraft[]) => Promise<ParkItemsBulkCreatePreviewResult>;
  @Input({ required: true }) applyHandler!: (rows: ParkItemBulkCreateDraft[]) => Promise<ParkItemsBulkCreateApplyResult>;
  @Input() currentLang: string = 'fr';

  @Output() applied: EventEmitter<ParkItemsBulkCreateApplyResult> =
    new EventEmitter<ParkItemsBulkCreateApplyResult>();

  protected readonly isOpen = signal(false);
  protected readonly rawText = signal('');
  protected readonly draftRows = signal<ParkItemBulkCreateDraft[]>([]);
  protected readonly preview = signal<ParkItemsBulkCreatePreviewResult | null>(null);
  protected readonly isPreviewing = signal(false);
  protected readonly isApplying = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly categoryOptions: ReadonlyArray<TranslationOption<ParkItemCategory>> =
    PARK_ITEM_CATEGORY_OPTIONS;
  protected readonly typeOptions: ReadonlyArray<TranslationOption<ParkItemType>> =
    PARK_ITEM_TYPE_OPTIONS;
  protected readonly canPreview = computed(() => this.draftRows().length > 0 && !this.isPreviewing());
  protected readonly canApply = computed(() => {
    const preview: ParkItemsBulkCreatePreviewResult | null = this.preview();
    return !!preview && preview.rows.some((row: ParkItemBulkCreatePreviewRow) => row.canApply) && !this.isApplying();
  });
  protected readonly zoneOptions = computed<Array<{ label: string; value: string | null }>>(() =>
    this.zones().map((zone: ParkZone) => ({
      label: resolveLocalizedValue(zone.names, this.currentLang) ?? zone.name ?? '',
      value: zone.id ?? null,
    })),
  );

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
  }

  updateRawText(value: string): void {
    this.rawText.set(value);
    this.preview.set(null);
    this.error.set(null);
    this.draftRows.set(parseParkItemPasteImport(value));
  }

  updateDraft(row: ParkItemBulkCreateDraft, change: Partial<ParkItemBulkCreateDraft>): void {
    this.preview.set(null);
    this.draftRows.update((rows: ParkItemBulkCreateDraft[]) =>
      rows.map((item: ParkItemBulkCreateDraft) =>
        item.rowNumber === row.rowNumber
          ? {
              ...item,
              ...change,
            }
          : item,
      ),
    );
  }

  removeDraft(row: ParkItemBulkCreateDraft): void {
    this.preview.set(null);
    this.draftRows.update((rows: ParkItemBulkCreateDraft[]) =>
      rows.filter((item: ParkItemBulkCreateDraft) => item.rowNumber !== row.rowNumber),
    );
  }

  async previewRows(): Promise<void> {
    if (!this.canPreview()) {
      return;
    }

    this.isPreviewing.set(true);
    this.error.set(null);
    try {
      this.preview.set(await this.previewHandler(this.draftRows()));
    } catch (error: unknown) {
      console.error('Error previewing park item paste import', error);
      this.error.set('admin.parks.items.import.error');
    } finally {
      this.isPreviewing.set(false);
    }
  }

  async applyRows(): Promise<void> {
    if (!this.canApply()) {
      return;
    }

    this.isApplying.set(true);
    this.error.set(null);
    try {
      const result: ParkItemsBulkCreateApplyResult = await this.applyHandler(this.draftRows());
      this.applied.emit(result);
      this.preview.set({
        rows: result.rows,
        readyCount: 0,
        warningCount: 0,
        errorCount: result.ignoredCount,
      });
      this.draftRows.set([]);
      this.rawText.set('');
      this.isOpen.set(false);
    } catch (error: unknown) {
      console.error('Error applying park item paste import', error);
      this.error.set('admin.parks.items.import.error');
    } finally {
      this.isApplying.set(false);
    }
  }
}
