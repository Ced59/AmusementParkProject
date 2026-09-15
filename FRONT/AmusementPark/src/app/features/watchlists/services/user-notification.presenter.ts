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

  formatValue(value: UserNotificationFactValue | null): string {
    if (!value) {
      return this.translateService.instant('notifications.values.notKnown');
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
          ...(value.kind === 'DateTime' ? { timeStyle: 'short' as const } : {})
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
