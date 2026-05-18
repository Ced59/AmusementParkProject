import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

export interface MapDirectionsTarget {
  latitude: number;
  longitude: number;
  label?: string | null;
}

@Injectable({ providedIn: 'root' })
export class MapDirectionsUrlService {
  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
  }

  buildDirectionsUrl(target: MapDirectionsTarget): string {
    const coordinates: string = `${target.latitude},${target.longitude}`;
    const label: string | null = this.normalizeLabel(target.label);

    if (this.shouldUseAppleMaps()) {
      const parameters: string[] = [`daddr=${encodeURIComponent(coordinates)}`];

      if (label) {
        parameters.push(`q=${encodeURIComponent(label)}`);
      }

      return `https://maps.apple.com/?${parameters.join('&')}`;
    }

    return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(coordinates)}`;
  }

  private shouldUseAppleMaps(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    const navigatorRef: Navigator = window.navigator;
    const userAgent: string = navigatorRef.userAgent ?? '';
    const platform: string = navigatorRef.platform ?? '';

    return /iPad|iPhone|iPod/.test(userAgent)
      || (platform === 'MacIntel' && navigatorRef.maxTouchPoints > 1);
  }

  private normalizeLabel(label: string | null | undefined): string | null {
    const normalizedLabel: string = label?.trim() ?? '';
    return normalizedLabel.length > 0 ? normalizedLabel : null;
  }
}
