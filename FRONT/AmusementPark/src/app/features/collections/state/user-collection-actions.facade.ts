import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';

import {
  UserCollectionEntry,
  UserCollectionKind,
  UserCollectionTargetType
} from '@app/models/watchlists/user-collection-entry.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import {
  USER_COLLECTIONS_DATA_PORT,
  UserCollectionsDataPort
} from './user-collections-data.port';

@Injectable()
export class UserCollectionActionsFacade {
  private readonly destroyRef: DestroyRef;
  private readonly targetTypeSignal = signal<UserCollectionTargetType>('Park');
  private readonly targetIdSignal = signal<string>('');
  private readonly entriesSignal = signal<UserCollectionEntry[]>([]);
  private readonly authenticatedSignal = signal<boolean>(false);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly mutatingKindSignal = signal<UserCollectionKind | null>(null);
  private readonly errorSignal = signal<boolean>(false);
  private loadRequestId: number = 0;
  private mutationRequestId: number = 0;

  readonly authenticated: Signal<boolean> = this.authenticatedSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly mutatingKind: Signal<UserCollectionKind | null> = this.mutatingKindSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(USER_COLLECTIONS_DATA_PORT) private readonly dataPort: UserCollectionsDataPort,
    private readonly authService: AuthService,
    sharedService: SharedService,
    destroyRef: DestroyRef
  ) {
    this.destroyRef = destroyRef;
    this.refreshAuthentication();
    sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((): void => {
        this.refreshAuthentication();
        if (this.authenticatedSignal()) {
          this.load();
        } else {
          this.invalidatePendingRequests();
          this.entriesSignal.set([]);
        }
      });
  }

  configure(targetType: UserCollectionTargetType, targetId: string): void {
    const normalizedTargetId: string = targetId.trim();
    const changed: boolean = targetType !== this.targetTypeSignal()
      || normalizedTargetId !== this.targetIdSignal();
    this.targetTypeSignal.set(targetType);
    this.targetIdSignal.set(normalizedTargetId);
    this.refreshAuthentication();
    if (changed) {
      this.invalidatePendingRequests();
      this.entriesSignal.set([]);
      this.errorSignal.set(false);
      if (this.authenticatedSignal() && normalizedTargetId) {
        this.load();
      }
    }
  }

  has(kind: UserCollectionKind): boolean {
    return this.entriesSignal().some(
      (entry: UserCollectionEntry): boolean => entry.kind === kind
    );
  }

  toggle(kind: UserCollectionKind): void {
    const targetId: string = this.targetIdSignal();
    if (!targetId || !this.authenticatedSignal() || this.mutatingKindSignal()) {
      return;
    }

    const targetType: UserCollectionTargetType = this.targetTypeSignal();
    const removing: boolean = this.has(kind);
    const requestId: number = ++this.mutationRequestId;
    this.mutatingKindSignal.set(kind);
    this.errorSignal.set(false);
    const request: Observable<UserCollectionEntry | void> = removing
      ? this.dataPort.delete(targetType, targetId, kind)
      : this.dataPort.add(targetType, targetId, kind);
    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (requestId === this.mutationRequestId) {
          this.mutatingKindSignal.set(null);
        }
      })
    ).subscribe({
      next: (entry: UserCollectionEntry | void): void => {
        if (!this.isCurrentTarget(requestId, targetType, targetId, false)) {
          return;
        }

        if (removing) {
          this.entriesSignal.update((entries: UserCollectionEntry[]) =>
            entries.filter((current: UserCollectionEntry): boolean => current.kind !== kind));
          return;
        }

        if (entry) {
          this.entriesSignal.update((entries: UserCollectionEntry[]) => [
            entry,
            ...entries.filter((current: UserCollectionEntry): boolean => current.kind !== kind)
          ]);
        }
      },
      error: (): void => {
        if (this.isCurrentTarget(requestId, targetType, targetId, false)) {
          this.errorSignal.set(true);
        }
      }
    });
  }

  private load(): void {
    const targetId: string = this.targetIdSignal();
    if (!targetId) {
      return;
    }

    const targetType: UserCollectionTargetType = this.targetTypeSignal();
    const requestId: number = ++this.loadRequestId;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.listMine(this.targetTypeSignal(), targetId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => {
          if (requestId === this.loadRequestId) {
            this.loadingSignal.set(false);
          }
        })
      )
      .subscribe({
        next: (entries: UserCollectionEntry[]): void => {
          if (this.isCurrentTarget(requestId, targetType, targetId, true)) {
            this.entriesSignal.set(entries);
          }
        },
        error: (): void => {
          if (this.isCurrentTarget(requestId, targetType, targetId, true)) {
            this.errorSignal.set(true);
          }
        }
      });
  }

  private invalidatePendingRequests(): void {
    this.loadRequestId++;
    this.mutationRequestId++;
    this.loadingSignal.set(false);
    this.mutatingKindSignal.set(null);
  }

  private isCurrentTarget(
    requestId: number,
    targetType: UserCollectionTargetType,
    targetId: string,
    loading: boolean
  ): boolean {
    const currentRequestId: number = loading ? this.loadRequestId : this.mutationRequestId;
    return requestId === currentRequestId
      && targetType === this.targetTypeSignal()
      && targetId === this.targetIdSignal();
  }

  private refreshAuthentication(): void {
    this.authenticatedSignal.set(this.authService.isLoggedIn());
  }
}
