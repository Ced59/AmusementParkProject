import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemCardViewModel } from '../models/park-item-card.model';
import { SelectOption } from '../models/select-option.model';
import { ParkItemsPageViewModel, ParkItemZoneCardViewModel } from '../models/park-items-page-view.model';
import { ParkItemsZoneFocusViewModel } from '../models/park-items-zone-focus.model';
import { ParkItemCardComponent } from './park-item-card.component';
import { ParkItemsFiltersComponent } from './park-items-filters.component';
import { ParkItemsZoneListComponent } from './park-items-zone-list.component';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiMapSlotComponent } from '@ui/maps';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-items-list-view',
  templateUrl: './park-items-list-view.component.html',
  styleUrls: ['./park-items-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    TranslateModule,
    EmptyStateComponent,
    PageStateComponent,
    PaginationComponent,
    ImageDisplayComponent,
    LeafletMapComponent,
    ParkItemCardComponent,
    ParkItemsFiltersComponent,
    ParkItemsZoneListComponent,
    UiMapSlotComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiStatCardComponent,
    UiSurfaceDirective,
    SafeRichHtmlPipe
  ]
})
export class ParkItemsListViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) pageView!: Signal<ParkItemsPageViewModel | null>;
  @Input({ required: true }) zoneCards!: Signal<ParkItemZoneCardViewModel[]>;
  @Input({ required: true }) zoneFocus!: Signal<ParkItemsZoneFocusViewModel | null>;
  @Input({ required: true }) pagedItems!: Signal<ParkItemCardViewModel[]>;
  @Input({ required: true }) searchTerm!: Signal<string>;
  @Input({ required: true }) selectedCategory!: Signal<string | null>;
  @Input({ required: true }) selectedType!: Signal<string | null>;
  @Input({ required: true }) selectedZoneId!: Signal<string | null>;
  @Input({ required: true }) categoryOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) typeOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) zoneOptions!: Signal<SelectOption[]>;
  @Input({ required: true }) totalResults!: Signal<number>;
  @Input({ required: true }) rangeStart!: Signal<number>;
  @Input({ required: true }) rangeEnd!: Signal<number>;
  @Input({ required: true }) currentPage!: Signal<number>;
  @Input({ required: true }) pageSize!: Signal<number>;

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearFiltersClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() searchChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() categoryChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() typeChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() zoneChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() zoneSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  protected readonly hasZones = computed(() => this.zoneCards().length > 0);

  private readonly mapMarkerPopupActionService: MapMarkerPopupActionService = inject(MapMarkerPopupActionService);
  private readonly translateService: TranslateService = inject(TranslateService);

  protected get zoneNavigationMarkers(): MapMarker[] {
    const focus: ParkItemsZoneFocusViewModel | null = this.zoneFocus();

    if (!focus) {
      return [];
    }

    const navigateLabel: string = this.translateService.instant('parks.map.navigate');
    const openDetailLabel: string = this.translateService.instant('parks.map.openDetail');

    return focus.map.markers.map((marker: MapMarker) => this.mapMarkerPopupActionService.enrich(marker, {
      directions: {
        latitude: marker.lat,
        longitude: marker.lng,
        label: marker.title
      },
      directionsLabel: navigateLabel,
      detailLabel: openDetailLabel
    }));
  }

  onBackClicked(): void {
    this.backClicked.emit();
  }

  clearFilters(): void {
    this.clearFiltersClicked.emit();
  }

  onSearchChanged(value: string): void {
    this.searchChanged.emit(value);
  }

  onCategoryChanged(value: string | null): void {
    this.categoryChanged.emit(value);
  }

  onTypeChanged(value: string | null): void {
    this.typeChanged.emit(value);
  }

  setMapTypeFilter(value: string | null): void {
    const nextValue: string | null = value && this.selectedType() === value ? null : value;
    this.typeChanged.emit(nextValue);
  }

  onZoneChanged(value: string | null): void {
    this.zoneChanged.emit(value);
  }

  onZoneSelected(zoneId: string | null): void {
    this.zoneSelected.emit(zoneId);
  }

  onPageChanged(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
