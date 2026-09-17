import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { UserNotificationFactValue } from '@app/models/watchlists/user-notification.model';
import { FactualEventType } from '@app/models/watchlists/watch-subscription.model';

@Injectable()
export class UserNotificationPresenter {
  constructor(private readonly translateService: TranslateService) {
  }

  eventLabel(eventType: FactualEventType): string {
    return this.translateService.instant(`notifications.events.${eventType}`);
  }

  formatValue(value: UserNotificationFactValue | null, eventType?: FactualEventType): string {
    if (!value) {
      return this.translateService.instant('notifications.values.notKnown');
    }

    if (this.isOpeningCalendarEvent(eventType) && value.kind === 'Text') {
      return this.formatOpeningCalendarSnapshot(value.canonicalValue)
        ?? this.translateService.instant('notifications.values.calendarUpdated');
    }

    if (value.kind === 'Boolean') {
      return this.translateService.instant(
        value.canonicalValue.toLowerCase() === 'true'
          ? 'notifications.values.yes'
          : 'notifications.values.no'
      );
    }

    if (value.kind === 'Date' || value.kind === 'DateTime') {
      const parsed: Date = new Date(value.canonicalValue);
      if (!Number.isNaN(parsed.getTime())) {
        return new Intl.DateTimeFormat(this.locale(), {
          dateStyle: 'long',
          ...(value.kind === 'DateTime'
            ? { timeStyle: 'short' as const }
            : { timeZone: 'UTC' })
        }).format(parsed);
      }
    }

    const numeric: number = Number(value.canonicalValue);
    if (value.kind === 'Money' && Number.isFinite(numeric) && value.unitCode) {
      return new Intl.NumberFormat(this.locale(), {
        style: 'currency',
        currency: value.unitCode
      }).format(numeric);
    }

    if ((value.kind === 'Integer' || value.kind === 'Decimal') && Number.isFinite(numeric)) {
      const formatted: string = new Intl.NumberFormat(this.locale()).format(numeric);
      return value.unitCode ? `${formatted} ${value.unitCode}` : formatted;
    }

    if (value.kind === 'Code') {
      return this.humanizeCode(value.canonicalValue);
    }

    return value.canonicalValue;
  }

  private locale(): string {
    return this.translateService.currentLang || this.translateService.defaultLang || 'en';
  }

  private isOpeningCalendarEvent(eventType: FactualEventType | undefined): boolean {
    return eventType === 'OpeningCalendarPublished' || eventType === 'OpeningCalendarChanged';
  }

  private formatOpeningCalendarSnapshot(canonicalValue: string): string | null {
    const fields: Map<string, string> = new Map<string, string>();
    canonicalValue.split(';').forEach((part: string): void => {
      const separator: number = part.indexOf('=');
      if (separator > 0) {
        fields.set(part.slice(0, separator), part.slice(separator + 1));
      }
    });
    if (fields.get('snapshot') !== '2') {
      return null;
    }

    const entries: string[] = (fields.get('entries') ?? '')
      .split('~')
      .map((entry: string): string => entry.trim())
      .filter((entry: string): boolean => entry.length > 0);
    const changedEntryCount: number = this.parsePositiveInteger(fields.get('changedEntryCount'));
    const totalEntryCount: number = this.parsePositiveInteger(fields.get('entryCount'));
    const displayedEntryCount: number = Math.min(
      entries.length,
      Math.max(1, Math.min(2, changedEntryCount || entries.length))
    );
    const presentedEntries: string[] = entries
      .slice(0, displayedEntryCount)
      .map((entry: string): string | null => this.formatCalendarEntry(entry))
      .filter((entry: string | null): entry is string => Boolean(entry));
    if (presentedEntries.length > 0) {
      const remaining: number = Math.max(0, totalEntryCount - presentedEntries.length);
      if (remaining > 0) {
        presentedEntries.push(this.translateService.instant(
          'notifications.values.calendarMore',
          { count: remaining }
        ));
      }
      return presentedEntries.join(' · ');
    }

    const coverage: string[] = (fields.get('coverage') ?? '').split('/');
    if (coverage.length === 2) {
      const start: string | null = this.formatCalendarDate(coverage[0]);
      const end: string | null = this.formatCalendarDate(coverage[1]);
      if (start && end) {
        return this.translateService.instant(
          'notifications.values.calendarCoverage',
          { start, end }
        );
      }
    }

    return null;
  }

  private formatCalendarEntry(serializedEntry: string): string | null {
    const fields: string[] = serializedEntry.split('|');
    if (fields.length < 8 || (fields[0] !== 'R' && fields[0] !== 'D')) {
      return null;
    }

    const start: string | null = this.formatCalendarDate(fields[1]);
    const end: string | null = this.formatCalendarDate(fields[2]);
    if (!start || !end) {
      return null;
    }

    const dateRange: string = fields[1] === fields[2]
      ? start
      : this.translateService.instant(
        'notifications.values.calendarCoverage',
        { start, end }
      );
    const days: string | null = fields[0] === 'R'
      ? this.formatCalendarDays(fields[3])
      : null;
    const schedule: string = fields[4] === 'C'
      ? this.translateService.instant('notifications.values.calendarClosed')
      : this.formatCalendarWindows(fields[6])
        ?? this.translateService.instant('notifications.values.calendarUpdated');
    return [dateRange, days, schedule]
      .filter((part: string | null): part is string => Boolean(part))
      .join(' · ');
  }

  private formatCalendarDate(value: string): string | null {
    const match: RegExpMatchArray | null = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (!match) {
      return null;
    }

    const date: Date = new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 12));
    return Number.isNaN(date.getTime())
      ? null
      : new Intl.DateTimeFormat(this.locale(), { dateStyle: 'medium', timeZone: 'UTC' }).format(date);
  }

  private formatCalendarDays(value: string): string | null {
    const labels: string[] = value
      .split(',')
      .map((day: string): number => Number(day))
      .filter((day: number): boolean => Number.isInteger(day) && day >= 0 && day <= 6)
      .map((day: number): string => new Intl.DateTimeFormat(
        this.locale(),
        { weekday: 'short', timeZone: 'UTC' }
      ).format(new Date(Date.UTC(2021, 7, 1 + day))));
    return labels.length > 0 ? labels.join('/') : null;
  }

  private formatCalendarWindows(value: string): string | null {
    const windows: string[] = value
      .split(',')
      .map((window: string): string | null => {
        const match: RegExpMatchArray | null = window.match(
          /^(\d{2}:\d{2})-(\d{2}:\d{2})(\+1)?(?:@\d{2}:\d{2}(?:\+1)?)?$/
        );
        return match ? `${match[1]}–${match[2]}${match[3] ? ' (+1)' : ''}` : null;
      })
      .filter((window: string | null): window is string => Boolean(window));
    return windows.length > 0 ? windows.join(', ') : null;
  }

  private parsePositiveInteger(value: string | undefined): number {
    const parsed: number = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : 0;
  }

  private humanizeCode(value: string): string {
    const words: string = value
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .trim();
    return words.length > 0
      ? words.charAt(0).toUpperCase() + words.slice(1).toLowerCase()
      : this.translateService.instant('notifications.values.notKnown');
  }
}
