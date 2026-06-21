import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PaginationContract } from '@shared/models/contracts';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiSearchPanelComponent, UiSearchPanelSelectFilterModel, UiSelectOptionModel } from '@ui/forms';
import { UiParkCardComponent } from '@ui/cards';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { ParkListMapComponent } from './park-list-map.component';

@Component({
  selector: 'app-park-list-view',
  templateUrl: './park-list-view.component.html',
  styleUrls: ['./park-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, PaginationComponent, NgFor, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective, UiSearchPanelComponent, UiParkCardComponent, ParkListMapComponent, PublicSharePanelComponent]
})
export class ParkListViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() mapState!: Signal<ScreenState<ParkMapPointViewModel[], string>>;
  @Input() parks!: Signal<ParkCardModel[]>;
  @Input() pagination!: Signal<PaginationContract | null>;
  @Input() visibleMapPoints!: Signal<ParkMapPointViewModel[]>;
  @Input() visibleCountryCount!: Signal<number>;
  @Input() selectedMapParkId!: Signal<string | null>;
  @Input() selectedParkCard!: Signal<ParkCardModel | null>;
  @Input() selectedRegion!: Signal<ParkRegionFilter | null>;
  @Input() selectedClosedFilter!: Signal<string>;
  @Input() closedFilterOptions!: Signal<UiSelectOptionModel[]>;
  @Input() currentLang!: Signal<string>;
  @Input() searchTerm!: Signal<string>;

  @Output() searchInputChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() clearSearchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() mapParkSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() regionFilterChanged: EventEmitter<ParkRegionFilter | null> = new EventEmitter<ParkRegionFilter | null>();
  @Output() closedFilterChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() resultParkFocused: EventEmitter<ParkCardModel> = new EventEmitter<ParkCardModel>();
  @Output() selectedParkCleared: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  protected readonly filters = computed<UiSearchPanelSelectFilterModel[]>(() => [
    {
      id: 'closed',
      labelKey: 'parks.closedFilters.label',
      selectedValue: this.selectedClosedFilter(),
      options: this.closedFilterOptions()
    }
  ]);

  protected buildParkLink(park: ParkCardModel): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: park.id,
      parkName: park.name
    });
  }

  onSearchInput(value: string): void {
    this.searchInputChanged.emit(value);
  }

  clearSearch(): void {
    this.clearSearchClicked.emit();
  }

  onMapParkSelected(parkId: string | null): void {
    this.mapParkSelected.emit(parkId);
  }

  onRegionFilterChanged(region: ParkRegionFilter | null): void {
    this.regionFilterChanged.emit(region);
  }

  onFilterChanged(event: { id: string; value: string | null }): void {
    if (event.id === 'closed') {
      this.closedFilterChanged.emit(event.value);
    }
  }

  onResultCardClick(event: MouseEvent, park: ParkCardModel): void {
    if (this.isInteractiveChildClick(event)) {
      return;
    }

    this.resultParkFocused.emit(park);
  }

  onResultCardKeydown(event: KeyboardEvent, park: ParkCardModel): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    this.resultParkFocused.emit(park);
  }

  private isInteractiveChildClick(event: MouseEvent): boolean {
    const target: EventTarget | null = event.target;
    const currentTarget: EventTarget | null = event.currentTarget;

    if (!(target instanceof HTMLElement) || !(currentTarget instanceof HTMLElement)) {
      return false;
    }

    const interactiveElement: Element | null = target.closest('a, button, input, textarea, select, [role=\"button\"]');
    return !!interactiveElement && interactiveElement !== currentTarget;
  }

  clearSelectedPark(): void {
    this.selectedParkCleared.emit();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
