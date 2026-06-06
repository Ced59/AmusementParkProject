import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { PaginatorState } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';
import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { AdminManufacturersStateFacade } from '@features/admin/manufacturers/state/admin-manufacturers-state.facade';

@Component({
  selector: 'app-admin-manufacturers',
  templateUrl: './admin-manufacturers.component.html',
  styleUrls: ['./admin-manufacturers.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminManufacturersStateFacade],
  imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, Tag, TranslateModule, EmptyStateComponent, PaginationComponent]
})
export class AdminManufacturersComponent implements OnInit {
  protected readonly manufacturers = this.stateFacade.pagedManufacturers;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalCount = this.stateFacade.totalCount;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  protected readonly selectedManufacturerIds = this.stateFacade.selectedManufacturerIds;
  protected readonly selectedCount = this.stateFacade.selectedCount;
  currentLang: string = 'en';

  constructor(
    protected readonly stateFacade: AdminManufacturersStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.stateFacade.loadManufacturers();
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }

  onAdminReviewStatusChanged(adminReviewStatus: AdminReviewStatus | null): void {
    this.stateFacade.setAdminReviewStatusFilter(adminReviewStatus);
  }

  onPageChanged(event: PaginatorState): void {
    const pageSize: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / pageSize) + 1;
    this.stateFacade.setPage(page, pageSize);
  }

  isManufacturerSelected(manufacturer: AttractionManufacturer): boolean {
    return !!manufacturer.id && this.selectedManufacturerIds().includes(manufacturer.id);
  }

  areAllCurrentManufacturersSelected(): boolean {
    const ids: string[] = this.manufacturers().map((manufacturer: AttractionManufacturer) => manufacturer.id).filter((id: string | undefined): id is string => !!id);
    return ids.length > 0 && ids.every((id: string) => this.selectedManufacturerIds().includes(id));
  }

  onManufacturerSelectionChange(manufacturer: AttractionManufacturer, event: Event): void {
    if (!manufacturer.id) {
      return;
    }

    this.stateFacade.setManufacturerSelection(manufacturer.id, (event.target as HTMLInputElement).checked);
  }

  onAllSelectionChange(event: Event): void {
    this.stateFacade.setCurrentPageSelection((event.target as HTMLInputElement).checked);
  }

  markSelectedToReview(): void {
    this.stateFacade.updateSelectedReviewStatus('ToReview');
  }

  markSelectedValidated(): void {
    this.stateFacade.updateSelectedReviewStatus('Validated');
  }

  markSelectedLater(): void {
    this.stateFacade.updateSelectedReviewStatus('ToProcessLater');
  }

  markSelectedNotRelevant(): void {
    this.stateFacade.updateSelectedReviewStatus('NotRelevant');
  }

  clearSelection(): void {
    this.stateFacade.clearSelection();
  }

  getStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }
}
