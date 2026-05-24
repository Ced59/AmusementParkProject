import { Component, Input } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { UiMapSlotComponent } from '@ui/maps';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { MapDirectionsUrlService } from '@shared/services/maps/map-directions-url.service';

@Component({
  selector: 'app-park-location-section',
  templateUrl: './park-location-section.component.html',
  styleUrls: ['./park-location-section.component.scss'],
  imports: [LeafletMapComponent, TranslateModule, UiMapSlotComponent]
})
export class ParkLocationSectionComponent {
  @Input() park: ParkDetailViewModel | null = null;

  constructor(
    private readonly mapDirectionsUrlService: MapDirectionsUrlService,
    private readonly translateService: TranslateService
  ) {
  }

  get hasCoordinates(): boolean {
    return this.park?.hasLocationInfo ?? false;
  }

  get center(): [number, number] {
    if (!this.park?.hasLocationInfo || this.park.latitude == null || this.park.longitude == null) {
      return [0, 0];
    }

    return [this.park.latitude, this.park.longitude];
  }

  get markers(): MapMarker[] {
    if (!this.park?.hasLocationInfo || this.park.latitude == null || this.park.longitude == null) {
      return [];
    }

    return [{
      id: this.park.id ?? 'park-location',
      lat: this.park.latitude,
      lng: this.park.longitude,
      title: this.park.name,
      subtitle: this.park.locationLine,
      iconKind: 'park',
      actionUrl: this.mapDirectionsUrlService.buildDirectionsUrl({
        latitude: this.park.latitude,
        longitude: this.park.longitude,
        label: this.park.name
      }),
      actionLabel: this.translateService.instant('parks.map.navigate')
    }];
  }
}
