import { Injectable } from '@angular/core';

import {
  buildParkItemMapDetailRouteCommands,
  buildParkMapDetailRouteCommands,
  ParkItemMapDetailTarget,
  ParkMapDetailTarget
} from './map-marker-detail-route.helpers';

@Injectable({ providedIn: 'root' })
export class MapMarkerDetailLinkService {
  buildParkDetailRouteCommands(target: ParkMapDetailTarget): string[] | null {
    return buildParkMapDetailRouteCommands(target);
  }

  buildParkItemDetailRouteCommands(target: ParkItemMapDetailTarget): string[] | null {
    return buildParkItemMapDetailRouteCommands(target);
  }
}
