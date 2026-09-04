import { describe, expect, it, vi } from 'vitest';

import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';
import { PassportAnonymousDraftStorePort } from './passport-anonymous-draft-store.ports';
import { PassportAnonymousDraftsStateFacade } from './passport-anonymous-drafts-state.facade';

describe('PassportAnonymousDraftsStateFacade', () => {
  it('removes a draft from the visible list only after its local deletion succeeds', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    const facade: PassportAnonymousDraftsStateFacade = new PassportAnonymousDraftsStateFacade(store, document);

    await facade.load();
    await facade.delete(draft.id);

    expect(store.delete).toHaveBeenCalledWith(draft.id);
    expect(facade.drafts()).toEqual([]);
    expect(facade.errorKey()).toBeNull();
  });

  it('keeps a draft visible when its local deletion fails', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    vi.mocked(store.delete).mockRejectedValueOnce(new Error('IndexedDB unavailable'));
    const facade: PassportAnonymousDraftsStateFacade = new PassportAnonymousDraftsStateFacade(store, document);

    await facade.load();
    await facade.delete(draft.id);

    expect(facade.drafts()).toEqual([draft]);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.errors.delete');
    expect(facade.mutating()).toBe(false);
  });

  it('reports unavailable browser storage without attempting to read it', async () => {
    const store: PassportAnonymousDraftStorePort = createStore([]);
    vi.mocked(store.isAvailable).mockReturnValue(false);
    const facade: PassportAnonymousDraftsStateFacade = new PassportAnonymousDraftsStateFacade(store, document);

    await facade.load();

    expect(store.list).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.errors.storageUnavailable');
  });
});

function createStore(drafts: PassportAnonymousDraft[]): PassportAnonymousDraftStorePort {
  return {
    isAvailable: vi.fn((): boolean => true),
    list: vi.fn(async (): Promise<PassportAnonymousDraft[]> => drafts),
    get: vi.fn(async (): Promise<PassportAnonymousDraft | null> => null),
    save: vi.fn(async (): Promise<void> => undefined),
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
