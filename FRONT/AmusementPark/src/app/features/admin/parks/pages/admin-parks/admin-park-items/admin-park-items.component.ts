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
import { ParkItemBulkFieldsUpdateRequest } from '@app/models/parks/park-item-bulk-fields-update-request';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkItemAdminSortField } from '@data-access/park-items/park-items-api-endpoints';
import { AdminParkItemsStateFacade } from '@features/admin/parks/state/admin-park-items-state.facade';
import { AdminParkItemsIndexViewComponent } from '@features/admin/park-items/pages/admin-park-items-index/admin-park-items-index-view.component';
import { AdminParkItemQuickCreateDrawerComponent } from '@app/components/admin/park-items/workbench/admin-park-item-quick-create-drawer.component';
import { AdminParkItemManufacturersStateFacade } from '@features/admin/park-items/state/admin-park-item-manufacturers-state.facade';
import { AdminParkItemWorkbenchStateFacade } from '@features/admin/park-items/workbench/state/admin-park-item-workbench-state.facade';
import {
  AdminParkItemQuickCreateDraft,
  AdminParkItemWorkbenchCoordinates
} from '@features/admin/park-items/workbench/models/admin-park-item-workbench.model';
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
  providers: [
    AdminParkItemsStateFacade,
    AdminParkItemManufacturersStateFacade,
    AdminParkItemWorkbenchStateFacade
  ],
  imports: [
    AdminParkItemQuickCreateDrawerComponent,
    AdminParkItemsIndexViewComponent
  ],
})
export class AdminParkItemsComponent implements OnInit {
  protected parkId: string = '';
  protected currentLang: string = 'en';
  protected readonly state = this.stateFacade.state;
  protected readonly loading = this.stateFacade.loading;
  protected readonly items = this.stateFacade.items;
  protected readonly zones = this.stateFacade.zones;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly searchTerm = this.stateFacade.searchTerm;
  protected readonly visibilityFilter = this.stateFacade.visibilityFilter;
  protected readonly adminReviewStatusFilter =
    this.stateFacade.adminReviewStatusFilter;
  protected readonly categoryFilter = this.stateFacade.categoryFilter;
  protected readonly typeFilter = this.stateFacade.typeFilter;
  protected readonly zoneIdFilter = this.stateFacade.zoneIdFilter;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly sortField = this.stateFacade.sortField;
  protected readonly sortOrder = this.stateFacade.sortOrder;
  protected readonly selectedItemIds = signal<string[]>([]);
  protected readonly selectedCount = computed(
    () => this.selectedItemIds().length,
  );
  protected readonly quickCreateOpen = signal(false);
  protected readonly quickCreateDraft = signal<AdminParkItemQuickCreateDraft | null>(null);
  protected readonly quickCreateFocusVersion = signal(0);
  protected readonly quickCreateDuplicateWarnings = this.workbenchStateFacade.duplicateWarnings;
  protected readonly isQuickCreating = this.workbenchStateFacade.isCreating;
  protected readonly manufacturerOptions = this.manufacturersStateFacade.manufacturerOptions;
  protected readonly zoneOptions = computed<
    Array<{ label: string; value: string | null }>
  >(() =>
    this.zones().map((zone: ParkZone) => ({
      label:
        resolveLocalizedValue(zone.names, this.currentLang) ??
        zone.name ??
        '',
      value: zone.id ?? null,
    })),
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
    private readonly manufacturersStateFacade: AdminParkItemManufacturersStateFacade,
    private readonly workbenchStateFacade: AdminParkItemWorkbenchStateFacade,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.quickCreateDraft.set(
      this.workbenchStateFacade.createDraftFromStoredContext(this.parkId),
    );
    this.manufacturersStateFacade.load();
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
    zoneId?: string | null;
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

  openQuickCreate(): void {
    this.quickCreateDraft.set(
      this.quickCreateDraft() ??
        this.workbenchStateFacade.createDraftFromStoredContext(this.parkId),
    );
    this.quickCreateOpen.set(true);
    this.bumpQuickCreateFocus();
    this.refreshDuplicateWarnings();
  }

  closeQuickCreate(): void {
    this.quickCreateOpen.set(false);
    this.workbenchStateFacade.clearDuplicateWarnings();
  }

  onQuickCreateDraftChanged(draft: AdminParkItemQuickCreateDraft): void {
    this.quickCreateDraft.set(draft);
    this.refreshDuplicateWarnings();
  }

  async createQuickItem(draft: AdminParkItemQuickCreateDraft): Promise<void> {
    await this.submitQuickCreate(draft, 'close');
  }

  async createQuickItemAndNew(
    draft: AdminParkItemQuickCreateDraft,
  ): Promise<void> {
    await this.submitQuickCreate(draft, 'new');
  }

  async createQuickItemAndOpen(
    draft: AdminParkItemQuickCreateDraft,
  ): Promise<void> {
    await this.submitQuickCreate(draft, 'open');
  }

  async duplicateItem(row: ParkItemAdminRow): Promise<void> {
    this.quickCreateDraft.set(
      this.workbenchStateFacade.createDraftFromRow(this.parkId, row),
    );
    this.quickCreateOpen.set(true);
    this.bumpQuickCreateFocus();
    this.refreshDuplicateWarnings();

    try {
      const draft: AdminParkItemQuickCreateDraft =
        await this.workbenchStateFacade.createDraftFromExistingItem(row);
      this.quickCreateDraft.set(draft);
      this.bumpQuickCreateFocus();
      this.refreshDuplicateWarnings();
    } catch (error: unknown) {
      console.error('Error preparing park item duplication', error);
    }
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

    await this.applyBulkFields({
      ids: this.selectedItemIds(),
      isVisible,
    });
  }

  async onBulkStatusChange(
    adminReviewStatus: AdminReviewStatus,
  ): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkFields({
      ids: this.selectedItemIds(),
      adminReviewStatus,
    });
  }

  async onBulkFieldsChange(
    request: ParkItemBulkFieldsUpdateRequest,
  ): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkFields({
      ...request,
      ids: this.selectedItemIds(),
    });
  }

  async onInlineFieldsChange(
    request: ParkItemBulkFieldsUpdateRequest,
  ): Promise<void> {
    if (request.ids.length === 0) {
      return;
    }

    await this.applyBulkFields(request, false);
  }

  clearSelection(): void {
    this.selectedItemIds.set([]);
  }

  private async submitQuickCreate(
    draft: AdminParkItemQuickCreateDraft,
    mode: 'close' | 'new' | 'open',
  ): Promise<void> {
    if (!draft.name.trim()) {
      return;
    }

    try {
      const createdItem: ParkItem =
        await this.workbenchStateFacade.createQuickItem(
          draft,
          this.getQuickCreateFallbackCoordinates(draft),
        );
      this.selectedItemIds.set([]);
      this.loadData(true);

      if (mode === 'new') {
        this.quickCreateDraft.set(
          this.workbenchStateFacade.createNextDraft(draft),
        );
        this.quickCreateOpen.set(true);
        this.bumpQuickCreateFocus();
        this.refreshDuplicateWarnings();
        return;
      }

      this.quickCreateOpen.set(false);
      this.workbenchStateFacade.clearDuplicateWarnings();

      if (mode === 'open' && createdItem.id) {
        this.router.navigate([
          '/',
          this.currentLang,
          'admin',
          'parks',
          'edit',
          this.parkId,
          'items',
          createdItem.id,
        ]);
      }
    } catch (error: unknown) {
      console.error('Error creating park item from workbench', error);
    }
  }

  private refreshDuplicateWarnings(): void {
    const draft: AdminParkItemQuickCreateDraft | null = this.quickCreateDraft();
    if (!draft) {
      this.workbenchStateFacade.clearDuplicateWarnings();
      return;
    }

    this.workbenchStateFacade.refreshDuplicateWarnings(draft, this.items());
  }

  private getQuickCreateFallbackCoordinates(
    draft: AdminParkItemQuickCreateDraft,
  ): AdminParkItemWorkbenchCoordinates {
    if (draft.coordinates) {
      return draft.coordinates;
    }

    const zone: ParkZone | undefined = this.stateFacade
      .zones()
      .find((item: ParkZone) => item.id === draft.zoneId);

    if (zone?.latitude != null && zone.longitude != null) {
      return {
        latitude: zone.latitude,
        longitude: zone.longitude,
      };
    }

    return {
      latitude: 0,
      longitude: 0,
    };
  }

  private bumpQuickCreateFocus(): void {
    this.quickCreateFocusVersion.update((value: number) => value + 1);
  }

  private async applyBulkFields(
    request: ParkItemBulkFieldsUpdateRequest,
    clearSelectionAfterSuccess: boolean = true,
  ): Promise<void> {
    const patch: Partial<Pick<ParkItemAdminRow, 'zoneId' | 'category' | 'type' | 'isVisible' | 'adminReviewStatus'>> =
      this.buildRowPatchFromBulkFields(request);

    try {
      if (Object.keys(patch).length > 0) {
        this.stateFacade.patchRows(request.ids, patch);
      }

      await firstValueFrom(
        this.stateFacade.updateBulkFields(request),
      );
      if (clearSelectionAfterSuccess) {
        this.selectedItemIds.set([]);
      }
    } catch (error: unknown) {
      console.error(
        'Error applying park item bulk fields update',
        error,
      );
      this.loadData(true);
    }
  }

  private buildRowPatchFromBulkFields(
    request: ParkItemBulkFieldsUpdateRequest,
  ): Partial<Pick<ParkItemAdminRow, 'zoneId' | 'category' | 'type' | 'isVisible' | 'adminReviewStatus'>> {
    const patch: Partial<Pick<ParkItemAdminRow, 'zoneId' | 'category' | 'type' | 'isVisible' | 'adminReviewStatus'>> = {};

    if (request.updateZone) {
      patch.zoneId = request.zoneId ?? null;
    }

    if (request.category) {
      patch.category = request.category;
    }

    if (request.type) {
      patch.type = request.type;
    }

    if (request.isVisible !== null && request.isVisible !== undefined) {
      patch.isVisible = request.isVisible;
    }

    if (request.adminReviewStatus) {
      patch.adminReviewStatus = request.adminReviewStatus;
    }

    return patch;
  }
}
