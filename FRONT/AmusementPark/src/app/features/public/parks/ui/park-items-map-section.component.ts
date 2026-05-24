import { ChangeDetectionStrategy, Component, Input, OnChanges, inject } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { UiButtonDirective, UiKickerComponent } from '@ui/primitives';
import { UiMapSlotComponent } from '@ui/maps';
import {
  ParkItemsMapFilterOptionViewModel,
  ParkItemsMapMarkerViewModel,
  ParkItemsMapViewModel
} from '../models/park-items-map-view.model';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';

@Component({
  selector: 'app-park-items-map-section',
  templateUrl: './park-items-map-section.component.html',
  styleUrls: ['./park-items-map-section.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiButtonDirective, UiKickerComponent, UiMapSlotComponent]
})
export class ParkItemsMapSectionComponent implements OnChanges {
  @Input() map: ParkItemsMapViewModel | null = null;

  private readonly mapMarkerPopupActionService: MapMarkerPopupActionService = inject(MapMarkerPopupActionService);
  private readonly translateService: TranslateService = inject(TranslateService);

  protected selectedCategory: string | null = null;
  protected selectedZoneId: string | null = null;
  protected navigationMarkers: MapMarker[] = [];

  ngOnChanges(): void {
    if (this.selectedCategory && !this.map?.categoryFilters.some((filter: ParkItemsMapFilterOptionViewModel) => filter.key === this.selectedCategory)) {
      this.selectedCategory = null;
    }

    if (this.selectedZoneId && !this.map?.zoneFilters.some((filter: ParkItemsMapFilterOptionViewModel) => filter.key === this.selectedZoneId)) {
      this.selectedZoneId = null;
    }

    this.refreshNavigationMarkers();
  }

  protected get hasZoneFilters(): boolean {
    return (this.map?.zoneFilters.length ?? 0) > 0;
  }

  protected get hasCategoryFilters(): boolean {
    return (this.map?.categoryFilters.length ?? 0) > 0;
  }

  protected get hasFilteredMarkers(): boolean {
    return this.filteredMarkers.length > 0;
  }

  protected get filteredMarkers(): ParkItemsMapMarkerViewModel[] {
    const sourceMarkers: ParkItemsMapMarkerViewModel[] = this.map?.markers ?? [];

    return sourceMarkers.filter((marker: ParkItemsMapMarkerViewModel) => {
      const categoryMatches: boolean = this.selectedCategory === null || marker.category === this.selectedCategory;
      const zoneMatches: boolean = this.selectedZoneId === null || marker.zoneId === this.selectedZoneId;
      return categoryMatches && zoneMatches;
    });
  }

  private refreshNavigationMarkers(): void {
    const currentMap: ParkItemsMapViewModel | null = this.map;

    if (!currentMap) {
      this.navigationMarkers = [];
      return;
    }

    const language: string = currentMap.language || this.translateService.currentLang;
    const navigateLabel: string = this.translateService.instant('parks.map.navigate');
    const openDetailLabel: string = this.translateService.instant('parks.map.openDetail');

    this.navigationMarkers = this.filteredMarkers.map((marker: ParkItemsMapMarkerViewModel) => {
      return this.mapMarkerPopupActionService.enrich(marker, {
        directions: {
          latitude: marker.lat,
          longitude: marker.lng,
          label: marker.title
        },
        directionsLabel: navigateLabel,
        parkItemDetail: {
          language,
          parkId: currentMap.parkId,
          parkName: currentMap.parkName,
          itemId: marker.itemId,
          itemName: marker.itemName
        },
        detailLabel: openDetailLabel
      });
    });
  }

  protected get center(): [number, number] {
    const markers: ParkItemsMapMarkerViewModel[] = this.filteredMarkers;

    if (markers.length > 0) {
      return [markers[0].lat, markers[0].lng];
    }

    return this.map?.center ?? [0, 0];
  }


  protected selectCategory(category: string | null): void {
    this.selectedCategory = category;
    this.refreshNavigationMarkers();
  }

  protected selectZone(zoneId: string | null): void {
    this.selectedZoneId = zoneId;
    this.refreshNavigationMarkers();
  }

  protected isSelectedCategory(category: string | null): boolean {
    return this.selectedCategory === category;
  }

  protected isSelectedZone(zoneId: string | null): boolean {
    return this.selectedZoneId === zoneId;
  }
}
