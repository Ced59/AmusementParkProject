import { Injectable } from '@angular/core';

import { MapMarker } from '@app/models/map/map-marker';
import { MapDirectionsUrlService, MapDirectionsTarget } from './map-directions-url.service';
import { MapMarkerDetailLinkService } from './map-marker-detail-link.service';
import { ParkItemMapDetailTarget, ParkMapDetailTarget } from './map-marker-detail-route.helpers';

export interface MapMarkerPopupActionOptions {
  directions?: MapDirectionsTarget | null;
  directionsLabel?: string | null;
  parkDetail?: ParkMapDetailTarget | null;
  parkItemDetail?: ParkItemMapDetailTarget | null;
  detailLabel?: string | null;
}

@Injectable({ providedIn: 'root' })
export class MapMarkerPopupActionService {
  constructor(
    private readonly mapDirectionsUrlService: MapDirectionsUrlService,
    private readonly mapMarkerDetailLinkService: MapMarkerDetailLinkService
  ) {
  }

  enrich<TMarker extends MapMarker>(marker: TMarker, options: MapMarkerPopupActionOptions): TMarker {
    const directionsUrl: string | null = this.resolveDirectionsUrl(options.directions ?? null);
    const detailRouteCommands: string[] | null = this.resolveDetailRouteCommands(options);

    return {
      ...marker,
      actionUrl: directionsUrl ?? marker.actionUrl ?? null,
      actionLabel: this.resolveLabel(options.directionsLabel, marker.actionLabel),
      directionsActionEnabled: marker.directionsActionEnabled ?? (options.directions != null),
      detailActionRouteCommands: detailRouteCommands ?? marker.detailActionRouteCommands ?? null,
      detailActionLabel: this.resolveLabel(options.detailLabel, marker.detailActionLabel),
      detailActionUrl: marker.detailActionUrl ?? null
    };
  }

  private resolveDirectionsUrl(target: MapDirectionsTarget | null): string | null {
    if (!target) {
      return null;
    }

    return this.mapDirectionsUrlService.buildDirectionsUrl(target);
  }

  private resolveDetailRouteCommands(options: MapMarkerPopupActionOptions): string[] | null {
    if (options.parkItemDetail) {
      return this.mapMarkerDetailLinkService.buildParkItemDetailRouteCommands(options.parkItemDetail);
    }

    if (options.parkDetail) {
      return this.mapMarkerDetailLinkService.buildParkDetailRouteCommands(options.parkDetail);
    }

    return null;
  }

  private resolveLabel(nextLabel: string | null | undefined, currentLabel: string | null | undefined): string | null {
    const normalizedNextLabel: string = nextLabel?.trim() ?? '';

    if (normalizedNextLabel.length > 0) {
      return normalizedNextLabel;
    }

    const normalizedCurrentLabel: string = currentLabel?.trim() ?? '';
    return normalizedCurrentLabel.length > 0 ? normalizedCurrentLabel : null;
  }
}
