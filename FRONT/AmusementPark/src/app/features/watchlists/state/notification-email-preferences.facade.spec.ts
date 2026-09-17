import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import {
  NotificationEmailPreference,
  NotificationEmailPreferenceUpdate
} from '@app/models/watchlists/notification-email-preference.model';
import { NotificationEmailPreferencesDataPort } from './notification-email-preferences-data.port';
import { NotificationEmailPreferencesFacade } from './notification-email-preferences.facade';

describe('NotificationEmailPreferencesFacade', () => {
  it('loads the consent state without exposing the full address', () => {
    const dataPort: NotificationEmailPreferencesDataPort = buildPort();
    const facade: NotificationEmailPreferencesFacade = createFacade(dataPort);

    facade.load();

    expect(dataPort.get).toHaveBeenCalledOnce();
    expect(facade.preference()?.maskedEmail).toBe('u***@example.com');
    expect(facade.loading()).toBe(false);
  });

  it('sends an explicit consent proof when email digests are enabled', () => {
    const dataPort: NotificationEmailPreferencesDataPort = buildPort();
    const facade: NotificationEmailPreferencesFacade = createFacade(dataPort);
    facade.load();

    facade.enable('fr');

    expect(dataPort.update).toHaveBeenCalledWith({
      emailDigestEnabled: true,
      consentAccepted: true,
      consentLocale: 'fr',
      expectedVersion: 3
    });
  });

  it('revokes consent without pretending to accept it again', () => {
    const dataPort: NotificationEmailPreferencesDataPort = buildPort(true);
    const facade: NotificationEmailPreferencesFacade = createFacade(dataPort);
    facade.load();

    facade.disable('fr');

    expect(dataPort.update).toHaveBeenCalledWith({
      emailDigestEnabled: false,
      consentAccepted: false,
      consentLocale: 'fr',
      expectedVersion: 3
    });
  });
});

function createFacade(
  dataPort: NotificationEmailPreferencesDataPort
): NotificationEmailPreferencesFacade {
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new NotificationEmailPreferencesFacade(dataPort, destroyRef as DestroyRef);
}

function buildPort(enabled: boolean = false): NotificationEmailPreferencesDataPort {
  const preference: NotificationEmailPreference = buildPreference(enabled);
  return {
    get: vi.fn().mockReturnValue(of(preference)),
    update: vi.fn().mockImplementation((input: NotificationEmailPreferenceUpdate) => of({
      ...preference,
      emailDigestEnabled: input.emailDigestEnabled,
      version: 4
    }))
  };
}

function buildPreference(enabled: boolean): NotificationEmailPreference {
  return {
    emailDigestEnabled: enabled,
    emailAvailable: true,
    maskedEmail: 'u***@example.com',
    consentTextVersion: 'watch-email-consent-v1',
    consentGrantedAtUtc: enabled ? '2026-09-17T08:00:00Z' : null,
    revokedAtUtc: null,
    version: 3
  };
}
