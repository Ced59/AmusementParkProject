import { describe, expect, it, vi } from 'vitest';

import { PassportProductAnalyticsPort } from '@core/analytics/passport-product-analytics.port';
import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';
import { PassportAnonymousDraftStorePort } from './passport-anonymous-draft-store.ports';
import { PassportAnonymousDraftsStateFacade } from './passport-anonymous-drafts-state.facade';

describe('PassportAnonymousDraftsStateFacade', () => {
  it('removes a draft from the visible list only after its local deletion succeeds', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    const analytics: PassportProductAnalyticsPort = { track: vi.fn() };
    const facade: PassportAnonymousDraftsStateFacade = createFacade(store, analytics);

    await facade.load();
    vi.mocked(analytics.track).mockClear();
    await facade.delete(draft.id);

    expect(store.delete).toHaveBeenCalledWith(draft.id);
    expect(facade.drafts()).toEqual([]);
    expect(facade.errorKey()).toBeNull();
    expect(vi.mocked(analytics.track).mock.calls).toEqual([
      [{ type: 'passport_deletion_started', source: 'anonymous-local' }],
      [{ type: 'passport_deletion_completed', source: 'anonymous-local' }]
    ]);
  });

  it('keeps a draft visible when its local deletion fails', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    vi.mocked(store.delete).mockRejectedValueOnce(new Error('IndexedDB unavailable'));
    const analytics: PassportProductAnalyticsPort = { track: vi.fn() };
    const facade: PassportAnonymousDraftsStateFacade = createFacade(store, analytics);

    await facade.load();
    vi.mocked(analytics.track).mockClear();
    await facade.delete(draft.id);

    expect(facade.drafts()).toEqual([draft]);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.errors.delete');
    expect(facade.mutating()).toBe(false);
    expect(analytics.track).toHaveBeenCalledTimes(1);
    expect(analytics.track).toHaveBeenCalledWith({
      type: 'passport_deletion_started',
      source: 'anonymous-local'
    });
  });

  it('reports unavailable browser storage without attempting to read it', async () => {
    const store: PassportAnonymousDraftStorePort = createStore([]);
    vi.mocked(store.isAvailable).mockReturnValue(false);
    const facade: PassportAnonymousDraftsStateFacade = createFacade(store);

    await facade.load();

    expect(store.list).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.errors.storageUnavailable');
  });

  it('protects a draft with an unfinished import from deletion and bulk clearing', async () => {
    const draft: PassportAnonymousDraft = {
      ...createDraft(),
      pendingImport: {
        choice: 'Separate',
        targetVisitId: 'server-1',
        metadataChoice: 'KeepServer',
        startedAtUtc: '2026-09-04T11:00:00.000Z'
      }
    };
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    const facade: PassportAnonymousDraftsStateFacade = createFacade(store);
    await facade.load();

    await facade.delete(draft.id);
    await facade.clear();

    expect(store.delete).not.toHaveBeenCalled();
    expect(store.clear).not.toHaveBeenCalled();
    expect(facade.hasLockedDrafts()).toBe(true);
    expect(facade.drafts()).toEqual([draft]);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.errors.importLocked');
  });
});

function createFacade(
  store: PassportAnonymousDraftStorePort,
  analytics: PassportProductAnalyticsPort = { track: vi.fn() }
): PassportAnonymousDraftsStateFacade {
  return new PassportAnonymousDraftsStateFacade(store, analytics, document);
}

function createStore(drafts: PassportAnonymousDraft[]): PassportAnonymousDraftStorePort {
  return {
    isAvailable: vi.fn((): boolean => true),
    list: vi.fn(async (): Promise<PassportAnonymousDraft[]> => drafts),
    get: vi.fn(async (): Promise<PassportAnonymousDraft | null> => null),
    save: vi.fn(async (): Promise<void> => undefined),
    claimSecondVisitMilestone: vi.fn(async (): Promise<boolean> => false),
    compareAndSet: vi.fn(async (): Promise<boolean> => true),
    deleteIfUnchanged: vi.fn(async (): Promise<boolean> => true),
    delete: vi.fn(async (): Promise<void> => undefined),
    clear: vi.fn(async (): Promise<void> => undefined)
  };
}

function createDraft(): PassportAnonymousDraft {
  return {
    schemaVersion: 1,
    id: 'draft-1',
    visitOperationId: 'visit-operation-1',
    rideOperationId: 'ride-operation-1',
    parkName: 'Parc test',
    visit: {
      parkId: 'park-1',
      date: { year: 2026, month: 9, day: 4, precision: 'Day', isApproximate: false },
      timeZoneId: null,
      serviceDayConvention: 'VisitStartLocalDate',
      title: null,
      privateNote: null
    },
    rides: [],
    createdAtUtc: '2026-09-04T10:00:00.000Z',
    updatedAtUtc: '2026-09-04T10:00:00.000Z'
  };
}
