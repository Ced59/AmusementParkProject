import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  ParkOpeningHoursCalendar,
  ParkOpeningHoursDay,
  ParkOpeningHoursTimeRange
} from '@app/models/parks/park-opening-hours';
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
    const parkNow: { localDate: string; minutes: number } = this.resolveParkNow(
      calendar.timeZoneId,
      new Date()
    );
    const previousDay: ParkOpeningHoursDay | null = calendar.days.find(
      (day: ParkOpeningHoursDay): boolean => day.localDate === this.addDaysToLocalDate(parkNow.localDate, -1)
    ) ?? null;
    if (this.findActiveRange(previousDay, parkNow.minutes + 1440, true) !== null) {
      return true;
    }

    for (const day of calendar.days ?? []) {
      if (day.isClosed || day.timeRanges.length === 0) {
        continue;
      }

      const dayOffset: number = this.diffLocalDatesInDays(parkNow.localDate, day.localDate);
      if (dayOffset < 0) {
        continue;
      }

      for (const range of day.timeRanges) {
        const opensAt: number = (dayOffset * 1440) + this.toMinutes(range.opensAt);
        const closesAt: number = (dayOffset * 1440) + this.toMinutes(range.closesAt)
          + (range.closesNextDay ? 1440 : 0);
        if (parkNow.minutes >= opensAt && parkNow.minutes < closesAt) {
          return true;
        }

        if (opensAt > parkNow.minutes) {
          return true;
        }
      }
    }

    return false;
  }

  private findActiveRange(
    day: ParkOpeningHoursDay | null,
    currentMinutes: number,
    requireNextDay: boolean
  ): ParkOpeningHoursTimeRange | null {
    if (!day || day.isClosed) {
      return null;
    }

    return day.timeRanges.find((range: ParkOpeningHoursTimeRange): boolean => {
      if (requireNextDay && !range.closesNextDay) {
        return false;
      }

      const opensAt: number = this.toMinutes(range.opensAt);
      const closesAt: number = this.toMinutes(range.closesAt) + (range.closesNextDay ? 1440 : 0);
      return currentMinutes >= opensAt && currentMinutes < closesAt;
    }) ?? null;
  }

  private resolveParkNow(
    timeZoneId: string | null | undefined,
    now: Date
  ): { localDate: string; minutes: number } {
    let parts: Intl.DateTimeFormatPart[];
    try {
      parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: timeZoneId || undefined,
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    } catch {
      parts = new Intl.DateTimeFormat('en-CA', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    }

    const valueByType: Record<string, string> = Object.fromEntries(
      parts.map((part: Intl.DateTimeFormatPart) => [part.type, part.value])
    );
    const hour: number = Number(valueByType['hour'] ?? '0');
    const minute: number = Number(valueByType['minute'] ?? '0');
    return {
      localDate: `${valueByType['year']}-${valueByType['month']}-${valueByType['day']}`,
      minutes: (hour * 60) + minute
    };
  }

  private addDaysToLocalDate(localDate: string, offset: number): string {
    const parts: number[] = localDate.split('-').map((part: string): number => Number(part));
    const date: Date = new Date(Date.UTC(parts[0], parts[1] - 1, parts[2]));
    date.setUTCDate(date.getUTCDate() + offset);
    return date.toISOString().slice(0, 10);
  }

  private diffLocalDatesInDays(fromLocalDate: string, toLocalDate: string): number {
    const fromParts: number[] = fromLocalDate.split('-').map((part: string): number => Number(part));
    const toParts: number[] = toLocalDate.split('-').map((part: string): number => Number(part));
    const fromTime: number = Date.UTC(fromParts[0], fromParts[1] - 1, fromParts[2]);
    const toTime: number = Date.UTC(toParts[0], toParts[1] - 1, toParts[2]);
    return Math.round((toTime - fromTime) / 86400000);
  }

  private toMinutes(value: string): number {
    const parts: number[] = value.split(':').map((part: string): number => Number(part));
    return ((parts[0] || 0) * 60) + (parts[1] || 0);
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
