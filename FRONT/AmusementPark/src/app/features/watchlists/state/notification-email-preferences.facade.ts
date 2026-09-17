import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import {
  NotificationEmailPreference,
  NotificationEmailPreferenceUpdate
} from '@app/models/watchlists/notification-email-preference.model';
import {
  NOTIFICATION_EMAIL_PREFERENCES_DATA_PORT,
  NotificationEmailPreferencesDataPort
} from './notification-email-preferences-data.port';

@Injectable()
export class NotificationEmailPreferencesFacade {
  private readonly preferenceSignal = signal<NotificationEmailPreference | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  readonly preference: Signal<NotificationEmailPreference | null> =
    this.preferenceSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(NOTIFICATION_EMAIL_PREFERENCES_DATA_PORT)
    private readonly dataPort: NotificationEmailPreferencesDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.get()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.loadingSignal.set(false))
      )
      .subscribe({
        next: (preference: NotificationEmailPreference): void =>
          this.preferenceSignal.set(preference),
        error: (): void => this.errorSignal.set(true)
      });
  }

  enable(consentLocale: string): void {
    this.update(true, true, consentLocale);
  }

  disable(consentLocale: string): void {
    this.update(false, false, consentLocale);
  }

  private update(
    emailDigestEnabled: boolean,
    consentAccepted: boolean,
    consentLocale: string
  ): void {
    const preference: NotificationEmailPreference | null = this.preferenceSignal();
    if (!preference || this.savingSignal()) {
      return;
    }

    const input: NotificationEmailPreferenceUpdate = {
      emailDigestEnabled,
      consentAccepted,
      consentLocale,
      expectedVersion: preference.version
    };
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.update(input)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.savingSignal.set(false))
      )
      .subscribe({
        next: (updated: NotificationEmailPreference): void =>
          this.preferenceSignal.set(updated),
        error: (): void => {
          this.errorSignal.set(true);
          this.load();
        }
      });
  }
}
