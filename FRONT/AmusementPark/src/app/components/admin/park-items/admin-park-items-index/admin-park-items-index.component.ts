import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { AdminParkItemsIndexStateFacade } from '@features/admin/park-items/state/admin-park-items-index-state.facade';
import { AdminParkItemsIndexViewComponent } from './admin-park-items-index-view.component';
import { getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';

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
  protected readonly selectedParkId = this.stateFacade.selectedParkId;
  protected readonly searchTerm = this.stateFacade.searchTerm;
  protected readonly pageSize = this.stateFacade.pageSize;

  constructor(
    private readonly stateFacade: AdminParkItemsIndexStateFacade,
    private readonly router: Router,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.initialize(this.translateService.instant('admin.parkItems.allParks'));
  }

  onFiltersChanged(filters: { selectedParkId: string | null; searchTerm: string }): void {
    this.stateFacade.updateFilters(filters.selectedParkId, filters.searchTerm);
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.stateFacade.updatePage(event);
  }

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    return typeof itemType === 'string'
      ? getParkItemTypeTranslationKey(itemType)
      : getParkItemTypeTranslationKey(null);
  }

  goToEdit(row: ParkItemAdminRow): void {
    const url: string = this.router.url;
    const lang: string = url.split('/')[1] || 'en';

    this.router.navigate(['/', lang, 'admin', 'parks', 'edit', row.parkId, 'items', row.id]);
  }
}
