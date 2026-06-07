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
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { PrimeTemplate } from 'primeng/api';
import { Tag } from 'primeng/tag';
import { ButtonDirective } from 'primeng/button';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import {
  AdminReviewStatus,
  getAdminReviewStatusSeverity,
  getAdminReviewStatusTranslationKey,
} from '@app/models/admin/admin-review-status';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import {
  PARK_ITEM_CATEGORY_OPTIONS,
  PARK_ITEM_TYPE_OPTIONS,
  TranslationOption,
} from '@shared/utils/display/display-options';
import { ParkItemAdminSortField } from '@data-access/park-items/park-items-api-endpoints';

interface PrimeSortEventLike {
  field?: string | string[] | null;
  order?: number | null;
}

@Component({
  selector: 'app-admin-park-items-index-view',
  templateUrl: './admin-park-items-index-view.component.html',
  styleUrls: ['./admin-park-items-index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Bind,
    Card,
    FormsModule,
    InputText,
    Select,
    TableModule,
    PrimeTemplate,
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
  @Input() showBulkActions: boolean = true;
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
    '—';

  @Output() filtersChanged: EventEmitter<{
    selectedParkId: string | null;
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
  }> = new EventEmitter<{
    selectedParkId: string | null;
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
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
  @Output() selectionCleared: EventEmitter<void> = new EventEmitter<void>();

  protected localSelectedParkId: string | null = null;
  protected localSearchTerm: string = '';
  protected localVisibilityFilter: boolean | null = null;
  protected localAdminReviewStatusFilter: AdminReviewStatus | null = null;
  protected localCategoryFilter: ParkItemCategory | null = null;
  protected localTypeFilter: ParkItemType | null = null;
  protected readonly categoryOptions: ReadonlyArray<
    TranslationOption<ParkItemCategory>
  > = PARK_ITEM_CATEGORY_OPTIONS;
  protected readonly typeOptions: ReadonlyArray<
    TranslationOption<ParkItemType>
  > = PARK_ITEM_TYPE_OPTIONS;

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

  onSortChange(event: PrimeSortEventLike): void {
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

  getPaginationFirst(): number {
    return Math.max(this.currentPage - 1, 0) * this.pageSize;
  }

  getColumnSpan(): number {
    let columnCount: number = 7;

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
      default:
        return 'default';
    }
  }
}
