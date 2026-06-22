import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

import { MapMarker } from '@app/models/map/map-marker';
import { MapDirectionsUrlService } from './map-directions-url.service';

@Injectable({ providedIn: 'root' })
export class MapMarkerPopupContentService {
  constructor(
    private readonly router: Router,
    private readonly translateService: TranslateService,
    private readonly mapDirectionsUrlService: MapDirectionsUrlService
  ) {
  }

  buildPopupContent(marker: MapMarker): string {
    const title: string = this.escapeHtml(marker.title ?? '');
    const subtitle: string = this.escapeHtml(this.resolveTranslatedPopupLine(marker.subtitleTranslationKey, marker.subtitle));
    const translatedDetails: string[] = (marker.detailTranslationKeys ?? [])
      .map((detailTranslationKey: string) => this.resolveTranslatedPopupLine(detailTranslationKey, null))
      .filter((detail: string) => detail.length > 0);
    const details: string[] = (marker.details ?? [])
      .map((detail: string) => detail.trim())
      .filter((detail: string) => detail.length > 0);

    const escapedTranslatedDetails: string[] = translatedDetails.map((detail: string) => this.escapeHtml(detail));
    const escapedDetails: string[] = details.map((detail: string) => this.escapeHtml(detail));
    const lines: string = [subtitle, ...escapedTranslatedDetails, ...escapedDetails]
      .filter((line: string) => line.length > 0)
      .map((line: string) => `<span>${line}</span>`)
      .join('');

    const actionLinks: string = this.buildPopupActions(marker);
    const titleBlock: string = title.length > 0 ? `<strong>${title}</strong>` : '';

    if (!titleBlock && !lines && !actionLinks) {
      return '';
    }

    if (!lines && !actionLinks) {
      return titleBlock;
    }

    const linesBlock: string = lines.length > 0 ? `<div class="leaflet-map-popup__lines">${lines}</div>` : '';
    return `${titleBlock}${linesBlock}${actionLinks}`;
  }

  private resolveTranslatedPopupLine(translationKey: string | null | undefined, fallback: string | null | undefined): string {
    const normalizedTranslationKey: string = translationKey?.trim() ?? '';

    if (normalizedTranslationKey.length > 0) {
      const translatedValue: string = this.translateService.instant(normalizedTranslationKey)?.trim() ?? '';

      if (translatedValue.length > 0 && translatedValue !== normalizedTranslationKey) {
        return translatedValue;
      }
    }

    return fallback?.trim() ?? '';
  }

  private buildPopupActions(marker: MapMarker): string {
    const detailActionLink: string = this.buildPopupDetailAction(marker);
    const directionsActionLink: string = this.buildPopupDirectionsAction(marker);
    const actionLinks: string = [detailActionLink, directionsActionLink]
      .filter((actionLink: string) => actionLink.length > 0)
      .join('');

    return actionLinks.length > 0
      ? `<div class="leaflet-map-popup__actions">${actionLinks}</div>`
      : '';
  }

  private buildPopupDetailAction(marker: MapMarker): string {
    const actionUrl: string = this.resolveDetailActionUrl(marker);
    const actionLabel: string = this.resolvePopupActionLabel(marker.detailActionLabel, 'parks.map.openDetail', actionUrl);

    if (!actionUrl || !actionLabel) {
      return '';
    }

    return `<a class="leaflet-map-popup__action leaflet-map-popup__action--detail" href="${this.escapeHtml(actionUrl)}" data-app-map-popup-internal-link="true">${this.escapeHtml(actionLabel)}</a>`;
  }

  private buildPopupDirectionsAction(marker: MapMarker): string {
    const actionUrl: string = this.resolveDirectionsActionUrl(marker);
    const actionLabel: string = this.resolvePopupActionLabel(marker.actionLabel, 'parks.map.navigate', actionUrl);

    if (!actionUrl || !actionLabel) {
      return '';
    }

    return `<a class="leaflet-map-popup__action leaflet-map-popup__action--directions" href="${this.escapeHtml(actionUrl)}" target="_blank" rel="noopener noreferrer">${this.escapeHtml(actionLabel)}</a>`;
  }

  private resolvePopupActionLabel(label: string | null | undefined, fallbackTranslationKey: string, actionUrl: string): string {
    const explicitLabel: string = label?.trim() ?? '';

    if (explicitLabel.length > 0) {
      return explicitLabel;
    }

    if (!actionUrl) {
      return '';
    }

    const translatedLabel: string = this.translateService.instant(fallbackTranslationKey);
    return translatedLabel?.trim() ?? '';
  }

  private resolveDirectionsActionUrl(marker: MapMarker): string {
    const explicitUrl: string = marker.actionUrl?.trim() ?? '';

    if (explicitUrl.length > 0) {
      return explicitUrl;
    }

    if (marker.directionsActionEnabled !== true || !Number.isFinite(marker.lat) || !Number.isFinite(marker.lng)) {
      return '';
    }

    return this.mapDirectionsUrlService.buildDirectionsUrl({
      latitude: marker.lat,
      longitude: marker.lng,
      label: marker.title ?? marker.subtitle ?? null
    });
  }

  private resolveDetailActionUrl(marker: MapMarker): string {
    const explicitUrl: string = marker.detailActionUrl?.trim() ?? '';

    if (explicitUrl.length > 0) {
      return explicitUrl;
    }

    const routeCommands: string[] | null | undefined = marker.detailActionRouteCommands;

    if (!routeCommands || routeCommands.length === 0) {
      return '';
    }

    return this.router.serializeUrl(this.router.createUrlTree(routeCommands));
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }
}
