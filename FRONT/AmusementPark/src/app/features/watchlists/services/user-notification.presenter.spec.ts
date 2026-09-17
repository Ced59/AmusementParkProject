import { TranslateService } from '@ngx-translate/core';

import { UserNotificationFactValue } from '@app/models/watchlists/user-notification.model';
import { UserNotificationPresenter } from './user-notification.presenter';

describe('UserNotificationPresenter', () => {
  it('turns an opening-calendar snapshot into readable localized evidence', () => {
    const presenter: UserNotificationPresenter = createPresenter();
    const value: UserNotificationFactValue = {
      kind: 'Text',
      canonicalValue: 'snapshot=2;timezone=Europe/Paris;coverage=2026-07-01/2026-07-31;rules=1;overrides=0;evidence=complete;entries=R|2026-07-01|2026-07-31|1|O|0|10:00-18:00|1|0;entryCount=1;changedEntryCount=1;sha256=internal-secret',
      unitCode: null
    };

    const result: string = presenter.formatValue(value, 'OpeningCalendarChanged');

    expect(result).toContain('2026');
    expect(result).toContain('10:00–18:00');
    expect(result).not.toContain('snapshot=');
    expect(result).not.toContain('sha256=');
    expect(result).not.toContain('internal-secret');
  });

  it('never exposes an unreadable technical calendar snapshot', () => {
    const presenter: UserNotificationPresenter = createPresenter();
    const value: UserNotificationFactValue = {
      kind: 'Text',
      canonicalValue: 'snapshot=2;sha256=internal-secret',
      unitCode: null
    };

    const result: string = presenter.formatValue(value, 'OpeningCalendarPublished');

    expect(result).toBe('Calendrier mis à jour');
  });
});

function createPresenter(): UserNotificationPresenter {
  const translations: Record<string, string> = {
    'notifications.values.calendarUpdated': 'Calendrier mis à jour',
    'notifications.values.calendarClosed': 'Fermé'
  };
  const translateService: Pick<TranslateService, 'currentLang' | 'defaultLang' | 'instant'> = {
    currentLang: 'fr',
    defaultLang: 'en',
    instant: (key: string, parameters?: Record<string, unknown>): string => {
      if (key === 'notifications.values.calendarCoverage') {
        return `${parameters?.['start']} – ${parameters?.['end']}`;
      }
      if (key === 'notifications.values.calendarMore') {
        return `+ ${parameters?.['count']} autres horaires`;
      }
      return translations[key] ?? key;
    }
  };
  return new UserNotificationPresenter(translateService as TranslateService);
}
