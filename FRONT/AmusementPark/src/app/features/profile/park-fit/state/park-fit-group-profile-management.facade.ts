import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DestroyRef, Inject, Injectable, PLATFORM_ID, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfileExport } from '@app/models/park-fit/park-fit-group-profile-export.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import {
  PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT,
  ParkFitGroupProfileManagementDataPort
} from './park-fit-group-profile-management-data.port';

export type ParkFitGroupProfileManagementStatus = 'idle' | 'loading' | 'ready' | 'error';

@Injectable()
export class ParkFitGroupProfileManagementFacade {
  private readonly profilesSignal = signal<ParkFitGroupProfile[]>([]);
  private readonly statusSignal = signal<ParkFitGroupProfileManagementStatus>('idle');
  private readonly savingSignal = signal<boolean>(false);
  private readonly deletingIdSignal = signal<string | null>(null);
  private readonly exportingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);

  public readonly profiles: Signal<ParkFitGroupProfile[]> = this.profilesSignal.asReadonly();
  public readonly status: Signal<ParkFitGroupProfileManagementStatus> = this.statusSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly deletingId: Signal<string | null> = this.deletingIdSignal.asReadonly();
  public readonly exporting: Signal<boolean> = this.exportingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();

  constructor(
    @Inject(PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT)
    private readonly dataPort: ParkFitGroupProfileManagementDataPort,
    private readonly destroyRef: DestroyRef,
    @Inject(PLATFORM_ID) private readonly platformId: object,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  load(): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.statusSignal.set('loading');
    this.errorKeySignal.set(null);
    this.dataPort.listMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profiles: ParkFitGroupProfile[]): void => {
          this.profilesSignal.set(profiles);
          this.statusSignal.set('ready');
        },
        error: (): void => {
          this.statusSignal.set('error');
          this.errorKeySignal.set('parkFitProfiles.feedback.loadError');
        }
      });
  }

  create(draft: ParkFitGroupProfileDraft): void {
    if (this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.errorKeySignal.set(null);
    this.dataPort.create(draft)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.savingSignal.set(false))
      )
      .subscribe({
        next: (profile: ParkFitGroupProfile): void => {
          this.profilesSignal.update((profiles: ParkFitGroupProfile[]) =>
            [profile, ...profiles]);
        },
        error: (): void => this.errorKeySignal.set('parkFitProfiles.feedback.saveError')
      });
  }

  update(profile: ParkFitGroupProfile, draft: ParkFitGroupProfileDraft): void {
    if (this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.errorKeySignal.set(null);
    this.dataPort.update(profile.profileId, profile.version, draft)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.savingSignal.set(false))
      )
      .subscribe({
        next: (updated: ParkFitGroupProfile): void => {
          this.profilesSignal.update((profiles: ParkFitGroupProfile[]) =>
            profiles.map((current: ParkFitGroupProfile): ParkFitGroupProfile =>
              current.profileId === updated.profileId ? updated : current));
        },
        error: (): void => this.errorKeySignal.set('parkFitProfiles.feedback.saveError')
      });
  }

  delete(profile: ParkFitGroupProfile): void {
    if (this.deletingIdSignal()) {
      return;
    }

    this.deletingIdSignal.set(profile.profileId);
    this.errorKeySignal.set(null);
    this.dataPort.delete(profile.profileId, profile.version)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.deletingIdSignal.set(null))
      )
      .subscribe({
        next: (): void => {
          this.profilesSignal.update((profiles: ParkFitGroupProfile[]) =>
            profiles.filter((current: ParkFitGroupProfile): boolean =>
              current.profileId !== profile.profileId));
        },
        error: (): void => this.errorKeySignal.set('parkFitProfiles.feedback.deleteError')
      });
  }

  export(): void {
    if (this.exportingSignal()) {
      return;
    }

    this.exportingSignal.set(true);
    this.errorKeySignal.set(null);
    this.dataPort.exportMine()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.exportingSignal.set(false))
      )
      .subscribe({
        next: (content: ParkFitGroupProfileExport): void => this.download(content),
        error: (): void => this.errorKeySignal.set('parkFitProfiles.feedback.exportError')
      });
  }

  private download(content: ParkFitGroupProfileExport): void {
    if (!isPlatformBrowser(this.platformId)
        || !this.document.defaultView
        || typeof URL === 'undefined'
        || typeof URL.createObjectURL !== 'function') {
      return;
    }

    const blob: Blob = new Blob(
      [JSON.stringify(content, null, 2)],
      { type: 'application/json;charset=utf-8' }
    );
    const objectUrl: string = URL.createObjectURL(blob);
    const anchor: HTMLAnchorElement = this.document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = `park-fit-profils-${content.exportedAtUtc.slice(0, 10)}.json`;
    anchor.rel = 'noopener';
    anchor.click();
    URL.revokeObjectURL(objectUrl);
  }
}
