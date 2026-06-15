import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { TableLazyLoadEvent } from 'primeng/table';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AdminParksStateFacade } from '@features/admin/parks/state/admin-parks-state.facade';
import { AdminParksViewComponent } from './admin-parks-view.component';
import { getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { ScrollAnchorService } from '@shared/services/scroll/scroll-anchor.service';

@Component({
  selector: 'app-admin-parks',
  templateUrl: './admin-parks.component.html',
  styleUrls: ['./admin-parks.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParksStateFacade],
  imports: [AdminParksViewComponent]
})
export class AdminParksComponent implements OnInit {
  protected readonly parks = this.stateFacade.parks;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly searchQuery = this.stateFacade.searchQuery;
  protected readonly visibilityFilter = this.stateFacade.visibilityFilter;
  protected readonly adminReviewStatusFilter = this.stateFacade.adminReviewStatusFilter;
  protected readonly typeFilter = this.stateFacade.typeFilter;
  protected readonly countryCodeFilter = this.stateFacade.countryCodeFilter;
  protected readonly validCoordinatesFilter = this.stateFacade.validCoordinatesFilter;
  protected readonly selectedParkIds = signal<string[]>([]);
  protected readonly selectedCount = computed(() => this.selectedParkIds().length);
  protected readonly canShowHeaderTotal = computed(() => !this.loading());
  protected readonly canClearSearch = computed(() => this.searchQuery().trim().length > 0);

  constructor(
    protected readonly stateFacade: AdminParksStateFacade,
    private readonly parksApiService: ParksApiService,
    private readonly scrollAnchorService: ScrollAnchorService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.loadParks(this.currentPage(), this.pageSize());
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }

  onSearch(): void {
    this.selectedParkIds.set([]);
    this.stateFacade.loadParks(1, this.pageSize());
  }

  clearSearch(): void {
    if (!this.canClearSearch()) {
      return;
    }

    this.stateFacade.clearSearchQuery();
    this.selectedParkIds.set([]);
    this.stateFacade.loadParks(1, this.pageSize());
  }

  onFiltersChanged(filters: ParkAdminListFilters): void {
    this.selectedParkIds.set([]);
    this.stateFacade.updateFilters(filters);
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;
    const shouldScroll: boolean = page !== this.currentPage() || rows !== this.pageSize();

    this.selectedParkIds.set([]);
    this.stateFacade.loadParks(page, rows);

    if (shouldScroll) {
      this.scrollAnchorService.scrollToSelector('[data-pagination-scroll-target="admin-parks"]');
    }
  }

  async onVisibilityChange(park: Park): Promise<void> {
    if (!park.id) {
      return;
    }

    const newValue: boolean = !!park.isVisible;

    try {
      await firstValueFrom(this.parksApiService.updateParkVisibility(park.id, newValue));
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    } catch (error: unknown) {
      console.error('Error updating park visibility', error);
      park.isVisible = !newValue;
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    }
  }

  onParkSelectionChanged(event: { parkId: string; selected: boolean }): void {
    const currentIds: string[] = this.selectedParkIds();
    if (event.selected) {
      this.selectedParkIds.set(currentIds.includes(event.parkId) ? currentIds : [...currentIds, event.parkId]);
      return;
    }

    this.selectedParkIds.set(currentIds.filter((parkId: string) => parkId !== event.parkId));
  }

  onAllParksSelectionChanged(selected: boolean): void {
    if (!selected) {
      this.selectedParkIds.set([]);
      return;
    }

    this.selectedParkIds.set(this.parks().map((park: Park) => park.id).filter((parkId: string | undefined): parkId is string => !!parkId));
  }

  async onBulkVisibilityChange(isVisible: boolean): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkAdministration({ isVisible });
  }

  async onBulkStatusChange(adminReviewStatus: AdminReviewStatus): Promise<void> {
    if (this.selectedCount() === 0) {
      return;
    }

    await this.applyBulkAdministration({ adminReviewStatus });
  }

  async onMakeFilteredValidCoordinateParksVisible(): Promise<void> {
    try {
      await firstValueFrom(this.stateFacade.makeFilteredValidCoordinateParksVisible());
      this.selectedParkIds.set([]);
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    } catch (error: unknown) {
      console.error('Error making valid-coordinate parks visible', error);
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    }
  }

  clearSelection(): void {
    this.selectedParkIds.set([]);
  }

  getTypeTranslationKey(type: string | null | undefined): string {
    return getParkTypeTranslationKey(type);
  }

  private async applyBulkAdministration(change: { isVisible?: boolean; adminReviewStatus?: AdminReviewStatus }): Promise<void> {
    try {
      await firstValueFrom(this.stateFacade.updateBulkAdministration({
        ids: this.selectedParkIds(),
        isVisible: change.isVisible ?? null,
        adminReviewStatus: change.adminReviewStatus ?? null
      }));
      this.selectedParkIds.set([]);
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    } catch (error: unknown) {
      console.error('Error applying bulk park administration update', error);
      this.stateFacade.loadParks(this.currentPage(), this.pageSize());
    }
  }
}
