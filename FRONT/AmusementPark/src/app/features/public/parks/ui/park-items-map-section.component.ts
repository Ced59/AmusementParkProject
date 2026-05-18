import { ChangeDetectionStrategy, Component, Input, OnChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { UiButtonDirective, UiKickerComponent } from '@ui/primitives';
import { UiMapSlotComponent } from '@ui/maps';
import {
  ParkItemsMapFilterOptionViewModel,
  ParkItemsMapMarkerViewModel,
  ParkItemsMapViewModel
} from '../models/park-items-map-view.model';

@Component({
  selector: 'app-park-items-map-section',
  templateUrl: './park-items-map-section.component.html',
  styleUrls: ['./park-items-map-section.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiButtonDirective, UiKickerComponent, UiMapSlotComponent]
})
export class ParkItemsMapSectionComponent implements OnChanges {
  @Input() map: ParkItemsMapViewModel | null = null;

  protected selectedCategory: string | null = null;
  protected selectedZoneId: string | null = null;

  ngOnChanges(): void {
    if (this.selectedCategory && !this.map?.categoryFilters.some((filter: ParkItemsMapFilterOptionViewModel) => filter.key === this.selectedCategory)) {
      this.selectedCategory = null;
    }

    if (this.selectedZoneId && !this.map?.zoneFilters.some((filter: ParkItemsMapFilterOptionViewModel) => filter.key === this.selectedZoneId)) {
      this.selectedZoneId = null;
    }
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

  protected get center(): [number, number] {
    const markers: ParkItemsMapMarkerViewModel[] = this.filteredMarkers;

    if (markers.length > 0) {
      return [markers[0].lat, markers[0].lng];
    }

    return this.map?.center ?? [0, 0];
  }

  protected selectCategory(category: string | null): void {
    this.selectedCategory = category;
  }

  protected selectZone(zoneId: string | null): void {
    this.selectedZoneId = zoneId;
  }

  protected isSelectedCategory(category: string | null): boolean {
    return this.selectedCategory === category;
  }

  protected isSelectedZone(zoneId: string | null): boolean {
    return this.selectedZoneId === zoneId;
  }
}
