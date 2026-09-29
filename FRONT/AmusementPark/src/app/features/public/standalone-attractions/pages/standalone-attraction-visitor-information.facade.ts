import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkOpeningHoursCalendar, ParkOpeningHoursDay } from '@app/models/parks/park-opening-hours';
import { ParkPricing } from '@app/models/parks/park-pricing';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ScreenState } from '@shared/models/contracts';
import {
  STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT,
  StandaloneAttractionVisitorInformationPort
} from './standalone-attraction-visitor-information.ports';

const OPENING_HOURS_PREVIEW_PAST_DAYS = 2;
const OPENING_HOURS_PREVIEW_FUTURE_DAYS = 14;
const OPENING_HOURS_NEXT_OPENING_LOOKAHEAD_DAYS = 370;

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
    const range: { from: string; to: string } = this.resolveOpeningHoursPreviewRange();
    this.port.getOpeningHours(attractionId, range.from, range.to, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (calendar: ParkOpeningHoursCalendar) => {
          if (this.requestedAttractionId !== attractionId) {
            return;
          }

          if (!this.hasCurrentOrFutureOpening(calendar)) {
            this.loadOpeningHoursNextOpeningWindow(attractionId, calendar);
            return;
          }

          this.openingHoursSignal.set(calendar);
          this.openingHoursStateSignal.set({ kind: 'ready', data: calendar });
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.openingHoursStateSignal.set({ kind: 'empty' });
          }
        }
      });
  }

  private loadOpeningHoursNextOpeningWindow(
    attractionId: string,
    previewCalendar: ParkOpeningHoursCalendar
  ): void {
    const range: { from: string; to: string } = this.resolveOpeningHoursNextOpeningRange();
    this.port.getOpeningHours(attractionId, range.from, range.to, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (futureCalendar: ParkOpeningHoursCalendar) => {
          if (this.requestedAttractionId !== attractionId) {
            return;
          }

          const calendar: ParkOpeningHoursCalendar = this.mergeOpeningHoursCalendars(
            previewCalendar,
            futureCalendar
          );
          this.openingHoursSignal.set(calendar);
          this.openingHoursStateSignal.set({ kind: 'ready', data: calendar });
        },
        error: () => {
          if (this.requestedAttractionId === attractionId) {
            this.openingHoursSignal.set(previewCalendar);
            this.openingHoursStateSignal.set({ kind: 'ready', data: previewCalendar });
          }
        }
      });
  }

  private resolveOpeningHoursPreviewRange(): { from: string; to: string } {
    const today: Date = new Date();
    return {
      from: this.formatLocalDate(this.addDays(today, -OPENING_HOURS_PREVIEW_PAST_DAYS)),
      to: this.formatLocalDate(this.addDays(today, OPENING_HOURS_PREVIEW_FUTURE_DAYS))
    };
  }

  private resolveOpeningHoursNextOpeningRange(): { from: string; to: string } {
    const today: Date = new Date();
    return {
      from: this.formatLocalDate(this.addDays(today, OPENING_HOURS_PREVIEW_FUTURE_DAYS + 1)),
      to: this.formatLocalDate(this.addDays(today, OPENING_HOURS_NEXT_OPENING_LOOKAHEAD_DAYS))
    };
  }

  private hasCurrentOrFutureOpening(calendar: ParkOpeningHoursCalendar): boolean {
    const today: string = this.formatLocalDate(new Date());
    return (calendar.days ?? []).some((day: ParkOpeningHoursDay): boolean => {
      return day.localDate >= today && !day.isClosed && day.timeRanges.length > 0;
    });
  }

  private mergeOpeningHoursCalendars(
    previewCalendar: ParkOpeningHoursCalendar,
    futureCalendar: ParkOpeningHoursCalendar
  ): ParkOpeningHoursCalendar {
    const daysByDate: Map<string, ParkOpeningHoursDay> = new Map<string, ParkOpeningHoursDay>();
    for (const day of [...(previewCalendar.days ?? []), ...(futureCalendar.days ?? [])]) {
      daysByDate.set(day.localDate, day);
    }

    return {
      ...previewCalendar,
      sourceUrl: previewCalendar.sourceUrl ?? futureCalendar.sourceUrl,
      notes: previewCalendar.notes ?? futureCalendar.notes,
      lastVerifiedAtUtc: previewCalendar.lastVerifiedAtUtc ?? futureCalendar.lastVerifiedAtUtc,
      updatedAtUtc: previewCalendar.updatedAtUtc || futureCalendar.updatedAtUtc,
      firstDate: previewCalendar.firstDate ?? futureCalendar.firstDate,
      lastDate: previewCalendar.lastDate ?? futureCalendar.lastDate,
      toDate: futureCalendar.toDate > previewCalendar.toDate
        ? futureCalendar.toDate
        : previewCalendar.toDate,
      days: [...daysByDate.values()].sort(
        (left: ParkOpeningHoursDay, right: ParkOpeningHoursDay): number => left.localDate.localeCompare(right.localDate)
      )
    };
  }

  private addDays(date: Date, offset: number): Date {
    const result: Date = new Date(date);
    result.setDate(result.getDate() + offset);
    return result;
  }

  private formatLocalDate(date: Date): string {
    const year: number = date.getFullYear();
    const month: string = String(date.getMonth() + 1).padStart(2, '0');
    const day: string = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
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
