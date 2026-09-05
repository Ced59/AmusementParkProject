import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

import {
  PassportGlobalStatisticsFilter,
  PassportGlobalStatisticsFilterStorePort
} from './passport-global-statistics-filter.ports';

@Injectable({ providedIn: 'root' })
export class PassportGlobalStatisticsFilterStoreService implements PassportGlobalStatisticsFilterStorePort {
  private readonly storageKey: string = 'passport-global-statistics-filter';

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
  }

  read(): PassportGlobalStatisticsFilter {
    if (!isPlatformBrowser(this.platformId)) {
      return { year: null, parkId: null };
    }

    try {
      const value: unknown = JSON.parse(sessionStorage.getItem(this.storageKey) ?? '{}');
      if (!this.isFilter(value)) {
        return { year: null, parkId: null };
      }
      return value;
    } catch {
      return { year: null, parkId: null };
    }
  }

  write(filter: PassportGlobalStatisticsFilter): void {
    if (isPlatformBrowser(this.platformId)) {
      try {
        sessionStorage.setItem(this.storageKey, JSON.stringify(filter));
      } catch {
        // Le filtre est un confort de session : son indisponibilité ne bloque jamais le passeport.
      }
    }
  }

  private isFilter(value: unknown): value is PassportGlobalStatisticsFilter {
    if (!value || typeof value !== 'object') {
      return false;
    }
    const candidate: Partial<PassportGlobalStatisticsFilter> =
      value as Partial<PassportGlobalStatisticsFilter>;
    const yearValid: boolean = candidate.year === null
      || (typeof candidate.year === 'number'
        && Number.isInteger(candidate.year)
        && candidate.year >= 1
        && candidate.year <= 9999);
    const parkValid: boolean = candidate.parkId === null || typeof candidate.parkId === 'string';
    return yearValid && parkValid;
  }
}
