import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';
import { MapMarker } from '../../../models/map/map-marker';
import { NgIf } from '@angular/common';
import { LeafletMapComponent } from '../../shared/leaflet-map/leaflet-map.component';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-location-section',
    templateUrl: './park-location-section.component.html',
    styleUrls: ['./park-location-section.component.scss'],
    imports: [NgIf, LeafletMapComponent, TranslateModule]
})
export class ParkLocationSectionComponent {
  @Input() park: Park | null = null;

  get hasCoordinates(): boolean {
    if (!this.park) {
      return false;
    }

    return Number.isFinite(this.park.latitude) && Number.isFinite(this.park.longitude);
  }

  get center(): [number, number] {
    if (!this.park) {
      return [0, 0];
    }

    return [this.park.latitude, this.park.longitude];
  }

  get markers(): MapMarker[] {
    if (!this.park || !this.hasCoordinates) {
      return [];
    }

    return [{
      id: this.park.id ?? 'park-location',
      lat: this.park.latitude,
      lng: this.park.longitude
    }];
  }
}
