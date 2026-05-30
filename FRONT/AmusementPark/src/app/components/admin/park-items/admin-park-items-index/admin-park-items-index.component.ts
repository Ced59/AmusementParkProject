import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkItemAdminSortField } from '@data-access/park-items/park-items-api-endpoints';
import { AdminParkItemsIndexStateFacade } from '@features/admin/park-items/state/admin-park-items-index-state.facade';
import { AdminParkItemsIndexViewComponent } from './admin-park-items-index-view.component';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
} from '@shared/utils/display/display-label.helpers';

@Component({
  selector: 'app-admin-park-items-index',
  templateUrl: './admin-park-items-index.component.html',
  styleUrls: ['./admin-park-items-index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkItemsIndexStateFacade],
  imports: [AdminParkItemsIndexViewComponent],
})
export class AdminParkItemsIndexComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly loading = this.stateFacade.loading;
  protected readonly rows = this.stateFacade.rows;
  protected readonly parkOptions = this.stateFacade.parkOptions;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly selectedParkId = this.stateFacade.selectedParkId;
  protected readonly searchTerm = this.stateFacade.searchTerm;
  protected readonly visibilityFilter = this.stateFacade.visibilityFilter;
  protected readonly adminReviewStatusFilter =
    this.stateFacade.adminReviewStatusFilter;
  protected readonly categoryFilter = this.stateFacade.categoryFilter;
  protected readonly typeFilter = this.stateFacade.typeFilter;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly sortField = this.stateFacade.sortField;
  protected readonly sortOrder = this.stateFacade.sortOrder;
  protected readonly selectedItemIds = signal<string[]>([]);
  protected readonly selectedCount = computed(
    () => this.selectedItemIds().length,
  );

  constructor(
    private readonly stateFacade: AdminParkItemsIndexStateFacade,
    private readonly router: Router,
    private readonly translateService: TranslateService,
  ) {}

  ngOnInit(): void {
    this.stateFacade.initialize(
      this.translateService.instant('admin.parkItems.allParks'),
    );
  }

  onFiltersChanged(filters: {
    selectedParkId: string | null;
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
  }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updateFilters(filters);
  }

  onPageChange(event: { page?: number; rows?: number; first?: number }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updatePage(event);
  }

  onSortChange(event: {
    sortBy: ParkItemAdminSortField;
    sortOrder: 1 | -1;
  }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updateSort(event);
  }

  getCategoryLabelKey(category: string | number | null | undefined): string {
    return typeof category === 'string'
      ? getParkItemCategoryTranslationKey(category)
      : getParkItemCategoryTranslationKey(null);
  }

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    return typeof itemType === 'string'
      ? getParkItemTypeTranslationKey(itemType)
      : getParkItemTypeTranslationKey(null);
  }

  goToEdit(row: ParkItemAdminRow): void {
    const url: string = this.router.url;
    const lang: string = url.split('/')[1] || 'en';

    this.router.navigate([
      '/',
      lang,
      'admin',
      'parks',
      'edit',
      row.parkId,
      'items',
      row.id,
    ]);
  }

  onItemSelectionChanged(event: { itemId: string; selected: boolean }): void {
    const currentIds: string[] = this.selectedItemIds();
    if (event.selected) {
      this.selectedItemIds.set(
        currentIds.includes(event.itemId)
          ? currentIds
          : [...currentIds, event.itemId],
      );
      return;
    }

    this.selectedItemIds.set(
      currentIds.filter((itemId: string) => itemId !== event.itemId),
    );
  }

  onAllItemsSelectionChanged(selected: boolean): void {
    if (!selected) {
      this.selectedItemIds.set([]);
      return;
    }

    this.selectedItemIds.set(
      this.rows()
        .map((row: ParkItemAdminRow) => row.id)
        .filter((itemId: string | undefined): itemId is string => !!itemId),
    );
  }

  async onBulkVisibilityChange(isVisible: boolean): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkAdministration({ isVisible });
  }

  async onBulkStatusChange(
    adminReviewStatus: AdminReviewStatus,
  ): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkAdministration({ adminReviewStatus });
  }

  clearSelection(): void {
    this.selectedItemIds.set([]);
  }

  private async applyBulkAdministration(change: {
    isVisible?: boolean;
    adminReviewStatus?: AdminReviewStatus;
  }): Promise<void> {
    try {
      await firstValueFrom(
        this.stateFacade.updateBulkAdministration({
          ids: this.selectedItemIds(),
          isVisible: change.isVisible ?? null,
          adminReviewStatus: change.adminReviewStatus ?? null,
        }),
      );
      this.selectedItemIds.set([]);
      this.stateFacade.loadData(true);
    } catch (error: unknown) {
      console.error(
        'Error applying bulk park item administration update',
        error,
      );
      this.stateFacade.loadData(true);
    }
  }
}
