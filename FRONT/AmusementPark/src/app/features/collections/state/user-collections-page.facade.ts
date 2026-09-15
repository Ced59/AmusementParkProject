import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import {
  USER_COLLECTIONS_DATA_PORT,
  UserCollectionsDataPort
} from './user-collections-data.port';

export type UserCollectionsPageStatus = 'idle' | 'loading' | 'ready' | 'error';

@Injectable()
export class UserCollectionsPageFacade {
  private readonly entriesSignal = signal<UserCollectionEntry[]>([]);
  private readonly statusSignal = signal<UserCollectionsPageStatus>('idle');
  private readonly removingIdSignal = signal<string | null>(null);
  private readonly actionErrorSignal = signal<boolean>(false);

  readonly entries: Signal<UserCollectionEntry[]> = this.entriesSignal.asReadonly();
  readonly status: Signal<UserCollectionsPageStatus> = this.statusSignal.asReadonly();
  readonly removingId: Signal<string | null> = this.removingIdSignal.asReadonly();
  readonly actionError: Signal<boolean> = this.actionErrorSignal.asReadonly();

  constructor(
    @Inject(USER_COLLECTIONS_DATA_PORT) private readonly dataPort: UserCollectionsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.statusSignal.set('loading');
    this.actionErrorSignal.set(false);
    this.dataPort.listMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (entries: UserCollectionEntry[]): void => {
          this.entriesSignal.set(entries);
          this.statusSignal.set('ready');
        },
        error: (): void => this.statusSignal.set('error')
      });
  }

  remove(entry: UserCollectionEntry): void {
    if (this.removingIdSignal()) {
      return;
    }

    this.removingIdSignal.set(entry.entryId);
    this.actionErrorSignal.set(false);
    this.dataPort.delete(entry.targetType, entry.targetId, entry.kind)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.removingIdSignal.set(null))
      )
      .subscribe({
        next: (): void => this.entriesSignal.update((entries: UserCollectionEntry[]) =>
          entries.filter((current: UserCollectionEntry): boolean => current.entryId !== entry.entryId)),
        error: (): void => this.actionErrorSignal.set(true)
      });
  }
}
