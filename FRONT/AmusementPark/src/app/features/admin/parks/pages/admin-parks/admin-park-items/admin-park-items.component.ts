import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  DestroyRef,
  computed,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkItemAdminSortField } from '@data-access/park-items/park-items-api-endpoints';
import { AdminParkItemsStateFacade } from '@features/admin/parks/state/admin-park-items-state.facade';
import { AdminParkItemsIndexViewComponent } from '@features/admin/park-items/pages/admin-park-items-index/admin-park-items-index-view.component';
import { resolveLocalizedValue } from '@shared/utils/localization';
import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
} from '@shared/utils/display/display-label.helpers';

@Component({
  selector: 'app-admin-park-items',
  templateUrl: './admin-park-items.component.html',
  styleUrls: ['./admin-park-items.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkItemsStateFacade],
  imports: [AdminParkItemsIndexViewComponent],
})
export class AdminParkItemsComponent implements OnInit {
  protected parkId: string = '';
  protected currentLang: string = 'en';
  protected readonly state = this.stateFacade.state;
  protected readonly loading = this.stateFacade.loading;
  protected readonly items = this.stateFacade.items;
  protected readonly totalRecords = this.stateFacade.totalRecords;
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
  protected readonly emptyParkOptions = signal<
    Array<{ label: string; value: string | null }>
  >([]);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly translateService: TranslateService,
    private readonly stateFacade: AdminParkItemsStateFacade,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.loadData();
  }

  loadData(forceReload: boolean = false): void {
    if (!this.parkId) {
      return;
    }

    this.stateFacade.loadData(this.parkId, forceReload);
  }

  onFiltersChanged(filters: {
    searchTerm: string;
    isVisible: boolean | null;
    adminReviewStatus: AdminReviewStatus | null;
    category: ParkItemCategory | null;
    type: ParkItemType | null;
  }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updateFilters(this.parkId, filters);
  }

  onPageChange(event: { page?: number; rows?: number; first?: number }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updatePage(this.parkId, event);
  }

  onSortChange(event: {
    sortBy: ParkItemAdminSortField;
    sortOrder: 1 | -1;
  }): void {
    this.selectedItemIds.set([]);
    this.stateFacade.updateSort(this.parkId, event);
  }

  getCreateRouterLink(): unknown[] {
    return [
      '/',
      this.currentLang,
      'admin',
      'parks',
      'edit',
      this.parkId,
      'items',
      'new',
    ];
  }

  getZoneName(zoneId?: string | null): string {
    if (!zoneId) {
      return '—';
    }

    const zone: ParkZone | undefined = this.stateFacade
      .zones()
      .find((item: ParkZone) => item.id === zoneId);
    return (
      resolveLocalizedValue(zone?.names, this.currentLang) ?? zone?.name ?? '—'
    );
  }

  getCategoryLabelKey(category: string | number | null | undefined): string {
    return typeof category === 'string'
      ? getParkItemCategoryTranslationKey(category)
      : getParkItemCategoryTranslationKey(null);
  }

  getTypeLabelKey(type: string | number | null | undefined): string {
    return typeof type === 'string'
      ? getParkItemTypeTranslationKey(type)
      : getParkItemTypeTranslationKey(null);
  }

  goToEdit(row: ParkItemAdminRow): void {
    if (!row.id) {
      return;
    }

    this.router.navigate([
      '/',
      this.currentLang,
      'admin',
      'parks',
      'edit',
      this.parkId,
      'items',
      row.id,
    ]);
  }

  deleteItem(row: ParkItemAdminRow): void {
    if (
      !row.id ||
      !confirm(this.translateService.instant('admin.parks.items.deleteConfirm'))
    ) {
      return;
    }

    this.parkItemsApiService
      .deleteParkItem(row.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadData(true),
        error: (error: unknown) =>
          console.error('Error deleting park item', error),
      });
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
      this.items()
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
      this.loadData(true);
    } catch (error: unknown) {
      console.error(
        'Error applying bulk park item administration update',
        error,
      );
      this.loadData(true);
    }
  }
}
