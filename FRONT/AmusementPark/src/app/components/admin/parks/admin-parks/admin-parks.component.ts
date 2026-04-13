import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { TableLazyLoadEvent } from 'primeng/table';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AdminParksStateFacade } from '@features/admin/parks/state/admin-parks-state.facade';
import { AdminParksViewComponent } from './admin-parks-view.component';
import { getParkTypeTranslationKey } from '@shared/utils/display/display-label.helpers';

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
  protected readonly canShowHeaderTotal = computed(() => !this.loading());
  protected readonly canClearSearch = computed(() => this.searchQuery().trim().length > 0);

  constructor(
    protected readonly stateFacade: AdminParksStateFacade,
    private readonly parksApiService: ParksApiService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.loadParks(this.currentPage(), this.pageSize());
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }

  onSearch(): void {
    this.stateFacade.loadParks(1, this.pageSize());
  }

  clearSearch(): void {
    if (!this.canClearSearch()) {
      return;
    }

    this.stateFacade.clearSearchQuery();
    this.stateFacade.loadParks(1, this.pageSize());
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    const rows: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;

    this.stateFacade.loadParks(page, rows);
  }

  onVisibilityChange(park: Park): void {
    if (!park.id) {
      return;
    }

    const newValue: boolean = !!park.isVisible;

    this.parksApiService.updateParkVisibility(park.id, newValue).subscribe({
      next: () => {
      },
      error: (error: unknown) => {
        console.error('Error updating park visibility', error);
        park.isVisible = !newValue;
        this.stateFacade.loadParks(this.currentPage(), this.pageSize());
      }
    });
  }

  getTypeTranslationKey(type: string | null | undefined): string {
    return getParkTypeTranslationKey(type);
  }
}
