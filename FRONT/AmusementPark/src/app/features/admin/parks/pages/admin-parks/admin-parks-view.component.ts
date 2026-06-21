import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Tag } from 'primeng/tag';
import { TranslateModule } from '@ngx-translate/core';
import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { ParkAdminListFilters, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-admin-parks-view',
  templateUrl: './admin-parks-view.component.html',
  styleUrls: ['./admin-parks.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, TableModule, ToggleSwitch, Tag, RouterLink, TranslateModule, EmptyStateComponent]
})
export class AdminParksViewComponent {
  @Input() parks!: Signal<Park[]>;
  @Input() loading!: Signal<boolean>;
  @Input() totalRecords!: Signal<number>;
  @Input() pageSize!: Signal<number>;
  @Input() currentPage!: Signal<number>;
  @Input() searchQuery!: Signal<string>;
  @Input() visibilityFilter!: Signal<boolean | null>;
  @Input() adminReviewStatusFilter!: Signal<AdminReviewStatus | null>;
  @Input() typeFilter!: Signal<ParkType | null>;
  @Input() countryCodeFilter!: Signal<string>;
  @Input() validCoordinatesFilter!: Signal<boolean | null>;
  @Input() sortField!: Signal<ParkAdminListSortField>;
  @Input() sortOrder!: Signal<1 | -1>;
  @Input() selectedParkIds!: Signal<string[]>;
  @Input() selectedCount!: Signal<number>;
  @Input() canShowHeaderTotal!: Signal<boolean>;
  @Input() canClearSearch!: Signal<boolean>;
  @Input() getTypeTranslationKeyFn: (type: string | null | undefined) => string = () => 'admin.parks.types.notSpecified';

  @Output() searchQueryChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() searchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearSearchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() filtersChanged: EventEmitter<ParkAdminListFilters> = new EventEmitter<ParkAdminListFilters>();
  @Output() pageChanged: EventEmitter<TableLazyLoadEvent> = new EventEmitter<TableLazyLoadEvent>();
  @Output() visibilityChanged: EventEmitter<Park> = new EventEmitter<Park>();
  @Output() parkSelectionChanged: EventEmitter<{ parkId: string; selected: boolean }> = new EventEmitter<{ parkId: string; selected: boolean }>();
  @Output() allParksSelectionChanged: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() bulkVisibilityChanged: EventEmitter<boolean> = new EventEmitter<boolean>();
  @Output() bulkStatusChanged: EventEmitter<AdminReviewStatus> = new EventEmitter<AdminReviewStatus>();
  @Output() bulkParkItemsVisibilityChanged: EventEmitter<void> = new EventEmitter<void>();
  @Output() filteredValidCoordinateParksVisibilityRequested: EventEmitter<void> = new EventEmitter<void>();
  @Output() selectionCleared: EventEmitter<void> = new EventEmitter<void>();

  protected localVisibilityFilter: boolean | null = null;
  protected localAdminReviewStatusFilter: AdminReviewStatus | null = null;
  protected localTypeFilter: ParkType | null = null;
  protected localCountryCodeFilter: string = '';
  protected localValidCoordinatesFilter: boolean | null = null;

  onSearchQueryChanged(searchQuery: string): void {
    this.searchQueryChanged.emit(searchQuery);
  }

  onSearch(): void {
    this.searchClicked.emit();
  }

  clearSearch(): void {
    this.clearSearchClicked.emit();
  }

  onFilterChanged(): void {
    this.filtersChanged.emit({
      isVisible: this.localVisibilityFilter,
      adminReviewStatus: this.localAdminReviewStatusFilter,
      type: this.localTypeFilter,
      countryCode: this.localCountryCodeFilter,
      hasValidCoordinates: this.localValidCoordinatesFilter
    });
  }

  onPageChanged(event: TableLazyLoadEvent): void {
    this.pageChanged.emit(event);
  }

  onVisibilityChange(park: Park): void {
    this.visibilityChanged.emit(park);
  }

  isParkSelected(park: Park): boolean {
    return !!park.id && this.selectedParkIds().includes(park.id);
  }

  areAllCurrentParksSelected(): boolean {
    const visibleIds: string[] = this.parks().map((park: Park) => park.id).filter((parkId: string | undefined): parkId is string => !!parkId);
    return visibleIds.length > 0 && visibleIds.every((parkId: string) => this.selectedParkIds().includes(parkId));
  }

  onParkSelectionChange(park: Park, event: Event): void {
    const selected: boolean = (event.target as HTMLInputElement).checked;
    if (!park.id) {
      return;
    }

    this.parkSelectionChanged.emit({ parkId: park.id, selected });
  }

  onAllSelectionChange(event: Event): void {
    this.allParksSelectionChanged.emit((event.target as HTMLInputElement).checked);
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

  makeSelectedParkItemsVisible(): void {
    this.bulkParkItemsVisibilityChanged.emit();
  }

  makeFilteredValidCoordinateParksVisible(): void {
    this.filteredValidCoordinateParksVisibilityRequested.emit();
  }

  clearSelection(): void {
    this.selectionCleared.emit();
  }

  getTypeTranslationKey(type: string | null | undefined): string {
    return this.getTypeTranslationKeyFn(type);
  }

  getStatusSeverity(status: AdminReviewStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    return getAdminReviewStatusSeverity(status);
  }

  getStatusLabelKey(status: AdminReviewStatus | null | undefined): string {
    return getAdminReviewStatusTranslationKey(status);
  }

  getParkGraphExportQueryParams(park: Park): Record<string, string> {
    return {
      parkId: park.id ?? '',
      parkName: park.name ?? '',
      parkCountryCode: park.countryCode ?? '',
      parkCity: park.city ?? '',
      parkLatitude: String(park.latitude),
      parkLongitude: String(park.longitude)
    };
  }

  hasValidCoordinates(park: Park): boolean {
    const latitude: number = Number(park.latitude);
    const longitude: number = Number(park.longitude);

    return Number.isFinite(latitude)
      && Number.isFinite(longitude)
      && (latitude !== 0 || longitude !== 0);
  }

  getCoordinatesLabel(park: Park): string {
    if (!this.hasValidCoordinates(park)) {
      return 'admin.parks.coordinatesMissing';
    }

    return `${park.latitude.toFixed(6)}, ${park.longitude.toFixed(6)}`;
  }

  getParkItemsTotalCountLabel(park: Park): string {
    return this.formatCount(park.parkItemsTotalCount);
  }

  getParkItemsVisibleCountLabel(park: Park): string {
    return this.formatCount(park.parkItemsVisibleCount);
  }

  private formatCount(count: number | null | undefined): string {
    return count === null || count === undefined ? '-' : String(count);
  }
}
