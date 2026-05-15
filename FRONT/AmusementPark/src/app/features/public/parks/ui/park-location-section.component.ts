import { Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { UiDistancePanelComponent, UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { ParkDetailViewModel } from '../models/park-detail-view.model';

@Component({
  selector: 'app-park-location-section',
  templateUrl: './park-location-section.component.html',
  styleUrls: ['./park-location-section.component.scss'],
  imports: [LeafletMapComponent, TranslateModule, UiDistancePanelComponent, UiMapShellComponent, UiMapSlotComponent]
})
export class ParkLocationSectionComponent {
  @Input() park: ParkDetailViewModel | null = null;

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
      lng: this.park.longitude
    }];
  }
}
