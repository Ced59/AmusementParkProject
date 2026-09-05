import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, Signal, computed, signal } from '@angular/core';

import {
  PASSPORT_PRODUCT_ANALYTICS_PORT,
  PassportProductAnalyticsPort
} from '@core/analytics/passport-product-analytics.port';
import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_STORE_PORT,
  PassportAnonymousDraftStorePort
} from './passport-anonymous-draft-store.ports';

@Injectable()
export class PassportAnonymousDraftsStateFacade {
  private readonly draftsSignal = signal<PassportAnonymousDraft[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly mutatingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private openedTracked = false;

  readonly drafts: Signal<PassportAnonymousDraft[]> = this.draftsSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly mutating: Signal<boolean> = this.mutatingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  readonly hasLockedDrafts: Signal<boolean> = computed((): boolean =>
    this.draftsSignal().some((draft: PassportAnonymousDraft): boolean => !!draft.pendingImport));

  constructor(
    @Inject(PASSPORT_ANONYMOUS_DRAFT_STORE_PORT)
    private readonly store: PassportAnonymousDraftStorePort,
    @Inject(PASSPORT_PRODUCT_ANALYTICS_PORT)
    private readonly productAnalytics: PassportProductAnalyticsPort,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
  }

  async load(): Promise<void> {
    if (!this.store.isAvailable()) {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.storageUnavailable');
      return;
    }

    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    try {
      this.draftsSignal.set(await this.store.list());
      if (!this.openedTracked) {
        this.openedTracked = true;
        this.productAnalytics.track({ type: 'passport_opened', source: 'anonymous-local' });
      }
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.load');
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async delete(draftId: string): Promise<void> {
    if (this.mutatingSignal()) {
      return;
    }

    const draft: PassportAnonymousDraft | undefined = this.draftsSignal().find(
      (candidate: PassportAnonymousDraft): boolean => candidate.id === draftId
    );
    if (draft?.pendingImport) {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.importLocked');
      return;
    }

    this.mutatingSignal.set(true);
    this.errorKeySignal.set(null);
    this.productAnalytics.track({ type: 'passport_deletion_started', source: 'anonymous-local' });
    try {
      await this.store.delete(draftId);
      this.draftsSignal.update((drafts: PassportAnonymousDraft[]): PassportAnonymousDraft[] =>
        drafts.filter((draft: PassportAnonymousDraft): boolean => draft.id !== draftId));
      this.productAnalytics.track({ type: 'passport_deletion_completed', source: 'anonymous-local' });
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.delete');
    } finally {
      this.mutatingSignal.set(false);
    }
  }

  async clear(): Promise<void> {
    if (this.mutatingSignal() || this.hasLockedDrafts()) {
      if (this.hasLockedDrafts()) {
        this.errorKeySignal.set('passport.anonymousDrafts.errors.importLocked');
      }
      return;
    }

    this.mutatingSignal.set(true);
    this.errorKeySignal.set(null);
    this.productAnalytics.track({ type: 'passport_deletion_started', source: 'anonymous-local' });
    try {
      await this.store.clear();
      this.draftsSignal.set([]);
      this.productAnalytics.track({ type: 'passport_deletion_completed', source: 'anonymous-local' });
    } catch {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.clear');
    } finally {
      this.mutatingSignal.set(false);
    }
  }

  export(): void {
    const defaultView: (Window & typeof globalThis) | null = this.document.defaultView;
    if (!defaultView || typeof defaultView.URL.createObjectURL !== 'function') {
      this.errorKeySignal.set('passport.anonymousDrafts.errors.export');
      return;
    }

    const exportedAtUtc: string = new Date().toISOString();
    const content: Blob = new Blob([
      JSON.stringify({
        schemaVersion: 1,
        exportedAtUtc,
        drafts: this.draftsSignal()
      }, null, 2)
    ], { type: 'application/json;charset=utf-8' });
    const url: string = defaultView.URL.createObjectURL(content);
    const anchor: HTMLAnchorElement = this.document.createElement('a');
    anchor.href = url;
    anchor.download = `amusement-park-passport-local-${exportedAtUtc.slice(0, 10)}.json`;
    anchor.rel = 'noopener';
    this.document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    defaultView.URL.revokeObjectURL(url);
    this.productAnalytics.track({
      type: 'passport_export_requested',
      source: 'anonymous-local',
      format: 'Json'
    });
  }

  rideCount(draft: PassportAnonymousDraft): number {
    return draft.rides.reduce(
      (total: number, ride): number => total + ride.count,
      0
    );
  }
}
