import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  Signal,
  SimpleChanges,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Select } from '@shared/ui/primitives/select';
import { TableModule } from '@shared/ui/primitives/table';
import { UiTemplate } from '@shared/ui/primitives/api';
import { Tag } from '@shared/ui/primitives/tag';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import {
  AdminReviewStatus,
  getAdminReviewStatusSeverity,
  getAdminReviewStatusTranslationKey,
} from '@app/models/admin/admin-review-status';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemBulkFieldsUpdateRequest } from '@app/models/parks/park-item-bulk-fields-update-request';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { DataCompletenessScore, getDataCompletenessLabel, getDataCompletenessSeverity } from '@app/models/shared/data-completeness-score';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import {
  PARK_ITEM_CATEGORY_OPTIONS,
  PARK_ITEM_TYPE_OPTIONS,
  TranslationOption,
} from '@shared/utils/display/display-options';
import {
  ParkItemAdminSortField,
  ParkItemContentBacklogFilter,
} from '@data-access/park-items/park-items-api-endpoints';

interface TableSortEventLike {
  field?: string | string[] | null;
  order?: number | null;
}

@Component({
  selector: 'app-admin-park-items-index-view',
  templateUrl: './admin-park-items-index-view.component.html',
  styleUrls: ['./admin-park-items-index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    FormsModule,
    InputText,
    Select,
    TableModule,
    UiTemplate,
    Tag,
    ButtonDirective,
    RouterLink,
    TranslateModule,
    PaginationComponent,
    EmptyStateComponent,
  ],
})
export class AdminParkItemsIndexViewComponent implements OnChanges {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() loading!: Signal<boolean>;
  @Input() rows!: Signal<ParkItemAdminRow[]>;
  @Input() parkOptions!: Signal<Array<{ label: string; value: string | null }>>;
  @Input() totalRecords!: Signal<number>;
  @Input() selectedParkId: string | null = null;
  @Input() searchTerm: string = '';
  @Input() visibilityFilter: boolean | null = null;
  @Input() adminReviewStatusFilter: AdminReviewStatus | null = null;
  @Input() categoryFilter: ParkItemCategory | null = null;
  @Input() typeFilter: ParkItemType | null = null;
  @Input() zoneIdFilter: string | null = null;
  @Input() contentBacklogFilter: ParkItemContentBacklogFilter | null = null;
  @Input() currentPage: number = 1;
  @Input() pageSize: number = 20;
  @Input() sortField: ParkItemAdminSortField = 'default';
  @Input() sortOrder: 1 | -1 = 1;
  @Input() selectedItemIds!: Signal<string[]>;
  @Input() selectedCount!: Signal<number>;
  @Input() titleKey: string = 'admin.parkItems.title';
  @Input() subtitleKey: string = 'admin.parkItems.subtitle';
  @Input() totalKey: string = 'admin.parkItems.total';
  @Input() searchPlaceholderKey: string = 'admin.parkItems.searchPlaceholder';
  @Input() emptyMessageKey: string = 'admin.parkItems.empty';
  @Input() showParkFilter: boolean = true;
  @Input() showParkColumn: boolean = true;
  @Input() showZoneColumn: boolean = false;
  @Input() showZoneFilter: boolean = false;
  @Input() showBulkActions: boolean = true;
  @Input() showFieldBulkActions: boolean = false;
  @Input() allowInlineEditing: boolean = false;
  @Input() showDeleteAction: boolean = false;
  @Input() showCreateButton: boolean = false;
  @Input() showQuickCreateButton: boolean = false;
  @Input() createButtonLabelKey: string = 'admin.parks.items.create';
  @Input() quickCreateButtonLabelKey: string = 'admin.parks.items.quickCreate.open';
  @Input() createButtonRouterLink: unknown[] | null = null;
  @Input() getCategoryLabelKeyFn: (
    category: string | number | null | undefined,
  ) => string = () => 'parkExplorer.categories.other';
  @Input() getTypeLabelKeyFn: (
    itemType: string | number | null | undefined,
  ) => string = () => 'parkExplorer.types.other';
  @Input() getZoneLabelFn: (zoneId: string | null | undefined) => string = () =>
    '-';

  @Input() zoneOptions: Signal<Array<{ label: string; value: string | null }>> | null = null;
  @Input() manufacturerOptions: Signal<EntitySelectOption[]> | null = null;

  @Output() filtersChanged: EventEmitter<{
    selectedParkId: string | null;
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
    zoneId: string | null;
    contentBacklogFilter: ParkItemContentBacklogFilter | null;
  }> = new EventEmitter<{
    selectedParkId: string | null;
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
    zoneId: string | null;
    contentBacklogFilter: ParkItemContentBacklogFilter | null;
  }>();
  @Output() pageChanged: EventEmitter<{
    page?: number;
    rows?: number;
    first?: number;
  }> = new EventEmitter<{ page?: number; rows?: number; first?: number }>();
  @Output() sortChanged: EventEmitter<{
    sortBy: ParkItemAdminSortField;
    sortOrder: 1 | -1;
  }> = new EventEmitter<{
    sortBy: ParkItemAdminSortField;
    sortOrder: 1 | -1;
  }>();
  @Output() editClicked: EventEmitter<ParkItemAdminRow> =
    new EventEmitter<ParkItemAdminRow>();
  @Output() quickDescriptionsClicked: EventEmitter<ParkItemAdminRow> =
    new EventEmitter<ParkItemAdminRow>();
  @Output() deleteClicked: EventEmitter<ParkItemAdminRow> =
    new EventEmitter<ParkItemAdminRow>();
  @Output() duplicateClicked: EventEmitter<ParkItemAdminRow> =
    new EventEmitter<ParkItemAdminRow>();
  @Output() quickCreateClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() itemSelectionChanged: EventEmitter<{
    itemId: string;
    selected: boolean;
  }> = new EventEmitter<{ itemId: string; selected: boolean }>();
  @Output() allItemsSelectionChanged: EventEmitter<boolean> =
    new EventEmitter<boolean>();
  @Output() bulkVisibilityChanged: EventEmitter<boolean> =
    new EventEmitter<boolean>();
  @Output() bulkStatusChanged: EventEmitter<AdminReviewStatus> =
    new EventEmitter<AdminReviewStatus>();
  @Output() bulkFieldsChanged: EventEmitter<ParkItemBulkFieldsUpdateRequest> =
    new EventEmitter<ParkItemBulkFieldsUpdateRequest>();
  @Output() inlineFieldsChanged: EventEmitter<ParkItemBulkFieldsUpdateRequest> =
    new EventEmitter<ParkItemBulkFieldsUpdateRequest>();
  @Output() selectionCleared: EventEmitter<void> = new EventEmitter<void>();

  protected localSelectedParkId: string | null = null;
  protected localSearchTerm: string = '';
  protected localVisibilityFilter: boolean | null = null;
  protected localAdminReviewStatusFilter: AdminReviewStatus | null = null;
  protected localCategoryFilter: ParkItemCategory | null = null;
  protected localTypeFilter: ParkItemType | null = null;
  protected localZoneIdFilter: string | null = null;
  protected localContentBacklogFilter: ParkItemContentBacklogFilter | null = null;
  protected bulkZoneId: string | null = null;
  protected bulkCategory: ParkItemCategory | null = null;
  protected bulkType: ParkItemType | null = null;
  protected bulkManufacturerId: string | null = null;
  protected readonly categoryOptions: ReadonlyArray<
    TranslationOption<ParkItemCategory>
  > = PARK_ITEM_CATEGORY_OPTIONS;
  protected readonly typeOptions: ReadonlyArray<
    TranslationOption<ParkItemType>
  > = PARK_ITEM_TYPE_OPTIONS;
  protected readonly contentBacklogFilterOptions: ReadonlyArray<{
    value: ParkItemContentBacklogFilter;
    labelKey: string;
  }> = [
    {
      value: 'MissingDescriptionFr',
      labelKey: 'admin.parkItems.content.filters.missingDescriptionFr',
    },
    {
      value: 'MissingDescriptionEn',
      labelKey: 'admin.parkItems.content.filters.missingDescriptionEn',
    },
    {
      value: 'MissingAnyDescription',
      labelKey: 'admin.parkItems.content.filters.missingAnyDescription',
    },
    {
      value: 'MissingZone',
      labelKey: 'admin.parkItems.content.filters.missingZone',
    },
    {
      value: 'MissingPreciseType',
      labelKey: 'admin.parkItems.content.filters.missingPreciseType',
    },
    {
      value: 'VisibleIncomplete',
      labelKey: 'admin.parkItems.content.filters.visibleIncomplete',
    },
  ];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedParkId']) {
      this.localSelectedParkId = this.selectedParkId;
    }
    if (changes['searchTerm']) {
      this.localSearchTerm = this.searchTerm;
    }
    if (changes['visibilityFilter']) {
      this.localVisibilityFilter = this.visibilityFilter;
    }
    if (changes['adminReviewStatusFilter']) {
      this.localAdminReviewStatusFilter = this.adminReviewStatusFilter;
    }
    if (changes['categoryFilter']) {
      this.localCategoryFilter = this.categoryFilter;
    }
    if (changes['typeFilter']) {
      this.localTypeFilter = this.typeFilter;
    }
    if (changes['zoneIdFilter']) {
      this.localZoneIdFilter = this.zoneIdFilter;
    }
    if (changes['contentBacklogFilter']) {
      this.localContentBacklogFilter = this.contentBacklogFilter;
    }
  }

  applyFilters(): void {
    this.filtersChanged.emit({
      selectedParkId: this.showParkFilter
        ? this.localSelectedParkId
        : this.selectedParkId,
      searchTerm: this.localSearchTerm,
      isVisible: this.localVisibilityFilter,
      adminReviewStatus: this.localAdminReviewStatusFilter,
      category: this.localCategoryFilter,
      type: this.localTypeFilter,
      zoneId: this.showZoneFilter ? this.localZoneIdFilter : null,
      contentBacklogFilter: this.localContentBacklogFilter,
    });
  }

  onImmediateFilterChanged(): void {
    this.applyFilters();
  }

  onPageChange(event: { page?: number; rows?: number; first?: number }): void {
    const rows: number = event.rows ?? this.pageSize;
    const pageIndex: number =
      event.page ?? Math.floor((event.first ?? 0) / Math.max(rows, 1));
    const first: number = event.first ?? pageIndex * rows;

    if (first === this.getPaginationFirst() && rows === this.pageSize) {
      return;
    }

    this.pageChanged.emit({
      page: pageIndex,
      rows,
      first,
    });
  }

  onSortChange(event: TableSortEventLike): void {
    const sortBy: ParkItemAdminSortField = this.mapTableSortField(event.field);
    const sortOrder: 1 | -1 = event.order === -1 ? -1 : 1;

    if (sortBy === this.sortField && sortOrder === this.sortOrder) {
      return;
    }

    this.sortChanged.emit({ sortBy, sortOrder });
  }

  getCategoryLabelKey(category: string | number | null | undefined): string {
    return this.getCategoryLabelKeyFn(category);
  }

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    return this.getTypeLabelKeyFn(itemType);
  }

  getZoneLabel(zoneId: string | null | undefined): string {
    return this.getZoneLabelFn(zoneId);
  }

  goToEdit(row: ParkItemAdminRow): void {
    this.editClicked.emit(row);
  }

  editQuickDescriptions(row: ParkItemAdminRow): void {
    this.quickDescriptionsClicked.emit(row);
  }

  deleteRow(row: ParkItemAdminRow): void {
    this.deleteClicked.emit(row);
  }

  duplicateRow(row: ParkItemAdminRow): void {
    this.duplicateClicked.emit(row);
  }

  openQuickCreate(): void {
    this.quickCreateClicked.emit();
  }

  isItemSelected(row: ParkItemAdminRow): boolean {
    return !!row.id && this.selectedItemIds().includes(row.id);
  }

  areAllCurrentItemsSelected(): boolean {
    const visibleIds: string[] = this.rows()
      .map((row: ParkItemAdminRow) => row.id)
      .filter((itemId: string | undefined): itemId is string => !!itemId);
    return (
      visibleIds.length > 0 &&
      visibleIds.every((itemId: string) =>
        this.selectedItemIds().includes(itemId),
      )
    );
  }

  onItemSelectionChange(row: ParkItemAdminRow, event: Event): void {
    if (!row.id) {
      return;
    }

    this.itemSelectionChanged.emit({
      itemId: row.id,
      selected: (event.target as HTMLInputElement).checked,
    });
  }

  onAllSelectionChange(event: Event): void {
    this.allItemsSelectionChanged.emit(
      (event.target as HTMLInputElement).checked,
    );
  }

  makeSelectedVisible(): void {
    this.bulkVisibilityChanged.emit(true);
  }

  hideSelected(): void {
    this.bulkVisibilityChanged.emit(false);
  }

  markSelectedToReview(): void {
    this.bulkStatusChanged.emit('ToReview');
  }

  markSelectedValidated(): void {
    this.bulkStatusChanged.emit('Validated');
  }

  markSelectedLater(): void {
    this.bulkStatusChanged.emit('ToProcessLater');
  }

  markSelectedNotRelevant(): void {
    this.bulkStatusChanged.emit('NotRelevant');
  }

  applyBulkZone(): void {
    this.bulkFieldsChanged.emit({
      ids: this.selectedItemIds(),
      updateZone: true,
      zoneId: this.bulkZoneId,
    });
  }

  applyBulkCategory(): void {
    if (!this.bulkCategory) {
      return;
    }

    this.bulkFieldsChanged.emit({
      ids: this.selectedItemIds(),
      category: this.bulkCategory,
    });
  }

  applyBulkType(): void {
    if (!this.bulkType) {
      return;
    }

    this.bulkFieldsChanged.emit({
      ids: this.selectedItemIds(),
      type: this.bulkType,
    });
  }

  applyBulkManufacturer(): void {
    this.bulkFieldsChanged.emit({
      ids: this.selectedItemIds(),
      updateManufacturer: true,
      manufacturerId: this.bulkManufacturerId,
    });
  }

  changeRowZone(row: ParkItemAdminRow, zoneId: string | null): void {
    this.inlineFieldsChanged.emit({
      ids: [row.id],
      updateZone: true,
      zoneId,
    });
  }

  changeRowCategory(row: ParkItemAdminRow, category: ParkItemCategory): void {
    this.inlineFieldsChanged.emit({
      ids: [row.id],
      category,
    });
  }

  changeRowType(row: ParkItemAdminRow, itemType: ParkItemType): void {
    this.inlineFieldsChanged.emit({
      ids: [row.id],
      type: itemType,
    });
  }

  changeRowVisibility(row: ParkItemAdminRow, event: Event): void {
    this.inlineFieldsChanged.emit({
      ids: [row.id],
      isVisible: (event.target as HTMLInputElement).checked,
    });
  }

  changeRowStatus(
    row: ParkItemAdminRow,
    adminReviewStatus: AdminReviewStatus,
  ): void {
    this.inlineFieldsChanged.emit({
      ids: [row.id],
      adminReviewStatus,
    });
  }

  clearSelection(): void {
    this.selectionCleared.emit();
  }

  getStatusSeverity(
    status: AdminReviewStatus | null | undefined,
  ): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }

  getQualitySeverity(row: ParkItemAdminRow): 'success' | 'info' | 'warn' | 'danger' {
    if (row.contentQuality?.isPublishable) {
      return 'success';
    }

    return row.isVisible ? 'danger' : 'warn';
  }

  getQualityLabelKey(row: ParkItemAdminRow): string {
    if (row.contentQuality?.isPublishable) {
      return 'admin.parkItems.content.complete';
    }

    return row.isVisible ? 'admin.parkItems.content.visibleIncomplete' : 'admin.parkItems.content.toComplete';
  }

  getLanguageCoverage(row: ParkItemAdminRow): string {
    const languages: string[] = row.contentQuality?.availableLanguageCodes ?? [];
    return languages.length > 0
      ? languages
          .map((languageCode: string) => languageCode.toUpperCase())
          .join(' | ')
      : '-';
  }

  getDataCompletenessLabel(score: DataCompletenessScore | null | undefined): string {
    return getDataCompletenessLabel(score);
  }

  getDataCompletenessSeverity(score: DataCompletenessScore | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getDataCompletenessSeverity(score);
  }

  getMissingRequirementKeys(row: ParkItemAdminRow): string[] {
    return row.contentQuality?.missingRequirementKeys ?? [];
  }

  getMissingRequirementLabelKey(requirement: string): string {
    return `admin.parkItems.content.requirements.${requirement}`;
  }

  getPaginationFirst(): number {
    return Math.max(this.currentPage - 1, 0) * this.pageSize;
  }

  getColumnSpan(): number {
    let columnCount: number = 9;

    if (this.showParkColumn) {
      columnCount += 1;
    }

    if (this.showZoneColumn) {
      columnCount += 1;
    }

    return columnCount;
  }

  private mapTableSortField(
    field: string | string[] | null | undefined,
  ): ParkItemAdminSortField {
    const rawField: string | undefined = Array.isArray(field)
      ? field[0]
      : (field ?? undefined);

    switch (rawField) {
      case 'name':
        return 'name';
      case 'type':
        return 'type';
      case 'category':
        return 'category';
      case 'adminReviewStatus':
        return 'adminReviewStatus';
      case 'isVisible':
        return 'isVisible';
      case 'parkName':
        return 'parkId';
      case 'zoneId':
        return 'zoneId';
      case 'dataCompletenessScore':
        return 'dataCompletenessScore';
      default:
        return 'default';
    }
  }
}
