import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableLazyLoadEvent, TableModule } from '@shared/ui/primitives/table';
import { Card } from '@shared/ui/primitives/card';
import { UiTemplate } from '@shared/ui/primitives/api';
import { InputText } from '@shared/ui/primitives/inputtext';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { ToggleSwitch } from '@shared/ui/primitives/toggleswitch';
import { Tag } from '@shared/ui/primitives/tag';
import { TranslateModule } from '@ngx-translate/core';
import { AdminReviewStatus, getAdminReviewStatusSeverity, getAdminReviewStatusTranslationKey } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkOpeningHoursAdminFilter, ParkOpeningHoursAdminStatus } from '@app/models/parks/park-opening-hours';
import { ParkType } from '@app/models/parks/park-type';
import { ParkAdminListFilters, ParkAdminListSortDirection, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';

type AdminParkSortOptionValue = `${ParkAdminListSortField}:${ParkAdminListSortDirection}`;

@Component({
  selector: 'app-admin-parks-view',
  templateUrl: './admin-parks-view.component.html',
  styleUrls: ['./admin-parks.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, UiTemplate, FormsModule, InputText, ButtonDirective, TableModule, ToggleSwitch, Tag, RouterLink, TranslateModule, EmptyStateComponent]
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
  @Input() audienceClassificationFilter!: Signal<ParkAudienceClassificationFilter | null>;
  @Input() countryCodeFilter!: Signal<string>;
  @Input() validCoordinatesFilter!: Signal<boolean | null>;
  @Input() openingHoursFilter!: Signal<ParkOpeningHoursAdminFilter>;
  @Input() sortField!: Signal<ParkAdminListSortField>;
  @Input() sortDirection!: Signal<ParkAdminListSortDirection>;
  @Input() sortOrder!: Signal<1 | -1>;
  @Input() selectedParkIds!: Signal<string[]>;
  @Input() selectedCount!: Signal<number>;
  @Input() canShowHeaderTotal!: Signal<boolean>;
  @Input() canClearSearch!: Signal<boolean>;
  @Input() getTypeTranslationKeyFn: (type: string | null | undefined) => string = () => 'admin.parks.types.notSpecified';
  @Input() getAudienceClassificationTranslationKeyFn: (classification: string | null | undefined) => string = () => 'admin.parks.audienceClassifications.notSpecified';

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
  @Output() sortChanged: EventEmitter<{ sortField: ParkAdminListSortField; sortDirection: ParkAdminListSortDirection }> = new EventEmitter<{ sortField: ParkAdminListSortField; sortDirection: ParkAdminListSortDirection }>();

  protected readonly mobileSortOptions: ReadonlyArray<{ value: AdminParkSortOptionValue; labelKey: string }> = [
    { value: 'default:asc', labelKey: 'admin.parks.sort.default' },
    { value: 'name:asc', labelKey: 'admin.parks.sort.nameAsc' },
    { value: 'parkItemsTotalCount:desc', labelKey: 'admin.parks.sort.totalDesc' },
    { value: 'parkItemsTotalCount:asc', labelKey: 'admin.parks.sort.totalAsc' },
    { value: 'parkItemsVisibleCount:desc', labelKey: 'admin.parks.sort.visibleDesc' },
    { value: 'parkItemsVisibleCount:asc', labelKey: 'admin.parks.sort.visibleAsc' },
    { value: 'openingHoursStatus:asc', labelKey: 'admin.parks.sort.openingHoursAttentionFirst' },
    { value: 'openingHoursStatus:desc', labelKey: 'admin.parks.sort.openingHoursReadyFirst' },
  ];

  protected localVisibilityFilter: boolean | null = null;
  protected localAdminReviewStatusFilter: AdminReviewStatus | null = null;
  protected localTypeFilter: ParkType | null = null;
  protected localAudienceClassificationFilter: ParkAudienceClassificationFilter | null = null;
  protected localCountryCodeFilter: string = '';
  protected localValidCoordinatesFilter: boolean | null = null;
  protected localOpeningHoursFilter: ParkOpeningHoursAdminFilter = 'all';

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
      audienceClassification: this.localAudienceClassificationFilter,
      countryCode: this.localCountryCodeFilter,
      hasValidCoordinates: this.localValidCoordinatesFilter,
      openingHoursStatus: this.localOpeningHoursFilter
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

  protected getSortValue(): AdminParkSortOptionValue {
    return `${this.sortField()}:${this.sortDirection()}` as AdminParkSortOptionValue;
  }

  protected onMobileSortChanged(value: string): void {
    const parts: string[] = value.split(':');
    this.sortChanged.emit({
      sortField: this.normalizeSortField(parts[0]),
      sortDirection: parts[1] === 'desc' ? 'desc' : 'asc',
    });
  }

  getTypeTranslationKey(type: string | null | undefined): string {
    return this.getTypeTranslationKeyFn(type);
  }

  getAudienceClassificationTranslationKey(classification: string | null | undefined): string {
    return this.getAudienceClassificationTranslationKeyFn(classification);
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

  getOpeningHoursStatusLabelKey(status: ParkOpeningHoursAdminStatus | null | undefined): string {
    return `admin.parks.openingHours.statuses.${status ?? 'NotConfigured'}`;
  }

  getOpeningHoursStatusSeverity(status: ParkOpeningHoursAdminStatus | null | undefined): 'success' | 'info' | 'warn' | 'danger' {
    switch (status) {
      case 'UpToDate':
        return 'success';
      case 'NeedsUpdate':
        return 'warn';
      case 'Expired':
        return 'danger';
      case 'NotConfigured':
      default:
        return 'info';
    }
  }

  getOpeningHoursCoverageLabelKey(park: Park): string {
    if (!park.openingHours?.hasOpeningHours) {
      return 'admin.parks.openingHours.notConfigured';
    }

    return park.openingHours.completeForDays === 1
      ? 'admin.parks.openingHours.coverageOne'
      : 'admin.parks.openingHours.coverageMany';
  }

  getOpeningHoursCoverageParams(park: Park): Record<string, string | number> {
    return {
      count: park.openingHours?.completeForDays ?? 0,
      date: park.openingHours?.completeUntilDate ?? park.openingHours?.lastDate ?? '-'
    };
  }

  private formatCount(count: number | null | undefined): string {
    return count === null || count === undefined ? '-' : String(count);
  }

  private normalizeSortField(value: string | undefined): ParkAdminListSortField {
    switch (value) {
      case 'name':
        return 'name';
      case 'parkItemsTotalCount':
        return 'parkItemsTotalCount';
      case 'parkItemsVisibleCount':
        return 'parkItemsVisibleCount';
      case 'openingHoursStatus':
        return 'openingHoursStatus';
      default:
        return 'default';
    }
  }
}
