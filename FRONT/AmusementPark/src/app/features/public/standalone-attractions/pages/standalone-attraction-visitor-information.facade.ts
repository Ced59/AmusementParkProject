import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkOpeningHoursCalendar } from '@app/models/parks/park-opening-hours';
import { ParkPricing } from '@app/models/parks/park-pricing';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ScreenState } from '@shared/models/contracts';
import {
  STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT,
  StandaloneAttractionVisitorInformationPort
} from './standalone-attraction-visitor-information.ports';

@Injectable()
export class StandaloneAttractionVisitorInformationFacade {
  private readonly openingHoursSignal = signal<ParkOpeningHoursCalendar | null>(null);
  private readonly pricingSignal = signal<ParkPricing | null>(null);
  private readonly weatherSignal = signal<ParkWeatherForecast | null>(null);
  private readonly openingHoursStateSignal = signal<ScreenState<ParkOpeningHoursCalendar, string>>({ kind: 'loading' });
  private readonly pricingStateSignal = signal<ScreenState<ParkPricing, string>>({ kind: 'loading' });
  private readonly weatherStateSignal = signal<ScreenState<ParkWeatherForecast, string>>({ kind: 'loading' });
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly port: StandaloneAttractionVisitorInformationPort = inject(
    STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT
  );
  private requestedAttractionId: string | null = null;

  readonly openingHours = this.openingHoursSignal.asReadonly();
  readonly pricing = this.pricingSignal.asReadonly();
  readonly weather = this.weatherSignal.asReadonly();
  readonly openingHoursState = this.openingHoursStateSignal.asReadonly();
  readonly pricingState = this.pricingStateSignal.asReadonly();
  readonly weatherState = this.weatherStateSignal.asReadonly();

  load(attractionId: string): void {
    const normalizedId: string = attractionId.trim();
    this.requestedAttractionId = normalizedId || null;
    this.reset();

    if (normalizedId.length === 0) {
      return;
    }

    this.loadOpeningHours(normalizedId);
    this.loadPricing(normalizedId);
    this.loadWeather(normalizedId);
  }

  private reset(): void {
    this.openingHoursSignal.set(null);
    this.pricingSignal.set(null);
    this.weatherSignal.set(null);
    this.openingHoursStateSignal.set({ kind: 'loading' });
    this.pricingStateSignal.set({ kind: 'loading' });
    this.weatherStateSignal.set({ kind: 'loading' });
  }

  private loadOpeningHours(attractionId: string): void {
    this.port.getOpeningHours(attractionId, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (calendar: ParkOpeningHoursCalendar) => {
          if (this.requestedAttractionId === attractionId) {
            this.openingHoursSignal.set(calendar);
            this.openingHoursStateSignal.set({ kind: 'ready', data: calendar });
          }
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.openingHoursStateSignal.set({ kind: 'empty' });
          }
        }
      });
  }

  private loadPricing(attractionId: string): void {
    this.port.getPricing(attractionId, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pricing: ParkPricing) => {
          if (this.requestedAttractionId === attractionId) {
            this.pricingSignal.set(pricing);
            this.pricingStateSignal.set({ kind: 'ready', data: pricing });
          }
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.pricingStateSignal.set({ kind: 'empty' });
          }
        }
      });
  }

  private loadWeather(attractionId: string): void {
    this.port.getWeather(attractionId, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (forecast: ParkWeatherForecast) => {
          if (this.requestedAttractionId === attractionId) {
            this.weatherSignal.set(forecast);
            this.weatherStateSignal.set(
              forecast.days.length > 0
                ? { kind: 'ready', data: forecast }
                : { kind: 'empty' }
            );
          }
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.weatherStateSignal.set({ kind: 'empty' });
          }
        }
      });
  }
}
