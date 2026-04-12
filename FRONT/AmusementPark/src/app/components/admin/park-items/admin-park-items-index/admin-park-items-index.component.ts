import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { ParkItemAdminRow } from '../../../../models/parks/park-item-admin-row';
import { AdminParkItemsIndexStateFacade } from '@features/admin/park-items/state/admin-park-items-index-state.facade';
import { AdminParkItemsIndexViewComponent } from './admin-park-items-index-view.component';

@Component({
  selector: 'app-admin-park-items-index',
  templateUrl: './admin-park-items-index.component.html',
  styleUrls: ['./admin-park-items-index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkItemsIndexStateFacade],
  imports: [AdminParkItemsIndexViewComponent]
})
export class AdminParkItemsIndexComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly rows = this.stateFacade.rows;
  protected readonly parkOptions = this.stateFacade.parkOptions;
  protected readonly totalRecords = this.stateFacade.totalRecords;
  selectedParkId: string | null = null;
  searchTerm: string = '';
  currentPage: number = 1;
  pageSize: number = 20;

  constructor(
    private readonly stateFacade: AdminParkItemsIndexStateFacade,
    private readonly router: Router,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.loadRows();
  }

  loadRows(): void {
    this.stateFacade.loadData(
      this.currentPage,
      this.pageSize,
      this.selectedParkId,
      this.searchTerm,
      this.translateService.instant('admin.parkItems.allParks')
    );
  }

  onFiltersChanged(): void {
    this.currentPage = 1;
    this.loadRows();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.loadRows();
  }

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    const normalizedType: string | null = typeof itemType === 'string'
      ? itemType
      : null;

    if (!normalizedType || normalizedType.length === 0) {
      return 'parkExplorer.types.other';
    }

    return `parkExplorer.types.${normalizedType.charAt(0).toLowerCase()}${normalizedType.slice(1)}`;
  }

  goToEdit(row: ParkItemAdminRow): void {
    const url: string = this.router.url;
    const lang: string = url.split('/')[1] || 'en';

    this.router.navigate(['/', lang, 'admin', 'parks', 'edit', row.parkId, 'items', row.id]);
  }
}
