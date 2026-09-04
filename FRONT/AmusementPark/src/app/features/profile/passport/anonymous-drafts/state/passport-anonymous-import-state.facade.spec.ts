import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import {
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrence
} from '@app/models/passport/passport-ride-occurrence.models';
import { ParkItem } from '@app/models/parks/park-item';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { PagedResult } from '@shared/models/contracts';
import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';
import { PassportAnonymousDraftStorePort } from './passport-anonymous-draft-store.ports';
import {
  PassportAnonymousImportOccurrencesPort,
  PassportAnonymousImportParkItemsPort,
  PassportAnonymousImportVisitsPort
} from './passport-anonymous-import-data.ports';
import { PassportAnonymousImportStateFacade } from './passport-anonymous-import-state.facade';

describe('PassportAnonymousImportStateFacade', () => {
  it('detects same-park same-date visits only after explicit comparison consent', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const existing: PassportVisit = createVisit({ id: 'existing-1' });
    const store: PassportAnonymousDraftStorePort = createStore([draft]);
    const createVisitRequest = vi.fn();
    const listVisits = vi.fn(() => of({ items: [existing], nextCursor: null }));
    const visits: PassportAnonymousImportVisitsPort = createVisitsPort({
      listVisits,
      createVisit: createVisitRequest
    });
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      store,
      visits,
      createOccurrencesPort(),
      createParkItemsPort()
    );

    await facade.load();

    expect(facade.previews()).toHaveLength(1);
    expect(facade.previews()[0].similarVisits).toEqual([]);
    expect(listVisits).not.toHaveBeenCalled();

    await facade.prepareComparison(true);

    expect(facade.previews()[0].similarVisits.map((visit: PassportVisit): string => visit.id))
      .toEqual(['existing-1']);
    expect(facade.previews()[0].decision.choice).toBe('Separate');
    expect(createVisitRequest).not.toHaveBeenCalled();
  });

  it('keeps the disclosure visible when a later draft comparison fails', async () => {
    const firstDraft: PassportAnonymousDraft = createDraft();
    const secondDraft: PassportAnonymousDraft = {
      ...createDraft(),
      id: 'draft-2',
      visitOperationId: 'visit-operation-2',
      rideOperationId: 'ride-operation-2'
    };
    const listVisits = vi.fn()
      .mockReturnValueOnce(of({ items: [], nextCursor: null }))
      .mockReturnValueOnce(throwError(() => new Error('comparison unavailable')));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([firstDraft, secondDraft]),
      createVisitsPort({ listVisits }),
      createOccurrencesPort(),
      createParkItemsPort()
    );
    await facade.load();

    await facade.prepareComparison(true);

    expect(listVisits).toHaveBeenCalledTimes(2);
    expect(facade.comparisonPrepared()).toBe(false);
    expect(facade.comparisonDataShared()).toBe(true);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.import.errors.preview');
  });

  it('imports a separate visit idempotently and purges local data only after verified acknowledgements', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const store: PassportAnonymousDraftStorePort = createStore([draft], deleteDraft);
    const createVisitRequest = vi.fn(() => of(createVisit({ id: 'server-1' })));
    const importBatch = vi.fn(() => of({
      occurrences: [
        createOccurrence('occurrence-1', 'server-1'),
        createOccurrence('occurrence-2', 'server-1')
      ],
      wasReplayed: false,
      wasOrderNormalized: false
    }));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      store,
      createVisitsPort({ createVisit: createVisitRequest }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(createVisitRequest).toHaveBeenCalledWith(draft.visit, draft.visitOperationId);
    expect(importBatch).toHaveBeenCalledTimes(1);
    expect(importBatch).toHaveBeenCalledWith(
      'server-1',
      expect.objectContaining({
        items: expect.arrayContaining([
          expect.objectContaining({
            moment: { localTime: '10:30:00', isApproximate: false }
          })
        ])
      }),
      draft.rideOperationId
    );
    expect(deleteDraft).toHaveBeenCalledWith(draft.id);
    expect(facade.report()).toMatchObject({
      importedVisitCount: 1,
      mergedVisitCount: 0,
      importedRideCount: 2,
      failedCount: 0
    });
    expect(facade.previews()).toEqual([]);
  });

  it('rejects a recategorized ride target before creating the separate visit', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const compareAndSet = vi.fn(async (): Promise<boolean> => true);
    const createVisitRequest = vi.fn(() => of(createVisit({ id: 'server-1' })));
    const importBatch = vi.fn();
    const getParkItemsByParkIdPage = vi.fn(() => of<PagedResult<ParkItem>>({
      items: [{
        id: draft.rides[0].parkItemId,
        parkId: draft.visit.parkId,
        name: draft.rides[0].attractionName,
        category: 'Restaurant',
        type: 'Restaurant',
        latitude: null,
        longitude: null
      }],
      pagination: { currentPage: 1, totalPages: 1, totalItems: 1, itemsPerPage: 100 }
    }));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], undefined, compareAndSet),
      createVisitsPort({ createVisit: createVisitRequest }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort({ getParkItemsByParkIdPage })
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(getParkItemsByParkIdPage).toHaveBeenCalledWith(
      draft.visit.parkId,
      1,
      100,
      { closedFilter: 'all', category: 'Attraction' },
      { closedFilter: 'all' }
    );
    expect(compareAndSet).not.toHaveBeenCalled();
    expect(createVisitRequest).not.toHaveBeenCalled();
    expect(importBatch).not.toHaveBeenCalled();
    expect(facade.previews()[0].draft.pendingImport).toBeUndefined();
    expect(facade.report()).toMatchObject({ failedCount: 1 });
  });

  it('recovers an ambiguous metadata merge before importing rides into the selected draft visit', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const existing: PassportVisit = createVisit({
      id: 'existing-1',
      title: 'Serveur',
      privateNote: 'Note serveur'
    });
    const recovered: PassportVisit = createVisit({ id: 'existing-1' });
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const updateVisit = vi.fn(() => throwError(() => new Error('lost response')));
    const getVisit = vi.fn(() => of(recovered));
    const importBatch = vi.fn(() => of({
      occurrences: [
        createOccurrence('occurrence-1', 'existing-1'),
        createOccurrence('occurrence-2', 'existing-1')
      ],
      wasReplayed: true,
      wasOrderNormalized: false
    }));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], deleteDraft),
      createVisitsPort({
        listVisits: vi.fn(() => of({ items: [existing], nextCursor: null })),
        updateVisit,
        getVisit
      }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);
    facade.setChoice(draft.id, 'Merge');
    await facade.setTargetVisit(draft.id, existing.id);
    facade.setMetadataChoice(draft.id, 'UseLocal');

    await facade.importAll(true);

    expect(updateVisit).toHaveBeenCalledTimes(1);
    expect(getVisit).toHaveBeenCalledWith(existing.id);
    expect(importBatch).toHaveBeenCalledWith(
      existing.id,
      expect.objectContaining({ items: expect.any(Array) }),
      expect.stringContaining(`${draft.rideOperationId}:merge:`)
    );
    expect(deleteDraft).toHaveBeenCalledWith(draft.id);
    expect(facade.report()).toMatchObject({ mergedVisitCount: 1, importedRideCount: 2 });
  });

  it('changes only disclosed title and note metadata during a merge', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const existing: PassportVisit = createVisit({
      id: 'existing-1',
      date: { ...draft.visit.date, isApproximate: true },
      timeZoneId: 'America/New_York',
      serviceDayConvention: 'UserSelectedServiceDate',
      title: 'Serveur',
      privateNote: 'Note serveur'
    });
    const updated: PassportVisit = createVisit({
      ...existing,
      title: draft.visit.title,
      privateNote: draft.visit.privateNote,
      version: 2
    });
    const updateVisit = vi.fn(() => of(updated));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft]),
      createVisitsPort({
        listVisits: () => of({ items: [existing], nextCursor: null }),
        getVisit: () => of(existing),
        updateVisit
      }),
      createOccurrencesPort({
        importBatch: () => of({
          occurrences: [
            createOccurrence('occurrence-1', existing.id),
            createOccurrence('occurrence-2', existing.id)
          ],
          wasReplayed: false,
          wasOrderNormalized: false
        })
      }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);
    facade.setChoice(draft.id, 'Merge');
    await facade.setTargetVisit(draft.id, existing.id);
    facade.setMetadataChoice(draft.id, 'UseLocal');

    await facade.importAll(true);

    expect(updateVisit).toHaveBeenCalledWith(existing.id, expect.objectContaining({
      date: existing.date,
      timeZoneId: existing.timeZoneId,
      serviceDayConvention: existing.serviceDayConvention,
      title: draft.visit.title,
      privateNote: draft.visit.privateNote
    }));
    expect(facade.report()).toMatchObject({ mergedVisitCount: 1, failedCount: 0 });
  });

  it('loads the complete private metadata before allowing a merge', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const listed: PassportVisit = createVisit({ id: 'existing-1', privateNote: null });
    const complete: PassportVisit = createVisit({
      id: 'existing-1',
      privateNote: 'Souvenir privé déjà enregistré'
    });
    const getVisit = vi.fn(() => of(complete));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft]),
      createVisitsPort({
        listVisits: vi.fn(() => of({ items: [listed], nextCursor: null })),
        getVisit
      }),
      createOccurrencesPort({ list: () => of({ items: [], nextCursor: null }) }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);
    facade.setChoice(draft.id, 'Merge');

    await facade.setTargetVisit(draft.id, listed.id);

    expect(getVisit).toHaveBeenCalledWith(listed.id);
    expect(facade.previews()[0].selectedTarget?.privateNote)
      .toBe('Souvenir privé déjà enregistré');
    expect(facade.previews()[0].serverRides).toEqual([]);
    expect(facade.canImport()).toBe(true);
  });

  it('blocks a merge when the target ride comparison cannot be loaded', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const listed: PassportVisit = createVisit({ id: 'existing-1' });
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft]),
      createVisitsPort({
        listVisits: vi.fn(() => of({ items: [listed], nextCursor: null })),
        getVisit: () => of(listed)
      }),
      createOccurrencesPort({
        list: () => throwError(() => new Error('comparison unavailable'))
      }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);
    facade.setChoice(draft.id, 'Merge');

    await facade.setTargetVisit(draft.id, listed.id);

    expect(facade.previews()[0].selectedTarget).toBeNull();
    expect(facade.previews()[0].serverRides).toBeNull();
    expect(facade.canImport()).toBe(false);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.import.errors.comparison');
  });

  it('keeps the local draft when a server acknowledgement cannot be verified', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const importBatch = vi.fn(() => of({
      occurrences: [createOccurrence('occurrence-1', 'wrong-visit')],
      wasReplayed: false,
      wasOrderNormalized: false
    }));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], deleteDraft),
      createVisitsPort(),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(deleteDraft).not.toHaveBeenCalled();
    expect(facade.report()).toMatchObject({ failedCount: 1 });
    expect(facade.previews()).toHaveLength(1);
  });

  it('keeps the local draft when an acknowledged ride differs from the submitted payload', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const changedOccurrence: PassportRideOccurrence = createOccurrence(
      'occurrence-2',
      'server-1',
      { privateNote: 'Note différente' }
    );
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], deleteDraft),
      createVisitsPort(),
      createOccurrencesPort({
        importBatch: () => of({
          occurrences: [createOccurrence('occurrence-1', 'server-1'), changedOccurrence],
          wasReplayed: false,
          wasOrderNormalized: false
        })
      }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(deleteDraft).not.toHaveBeenCalled();
    expect(facade.report()).toMatchObject({ failedCount: 1 });
    expect(facade.previews()).toHaveLength(1);
  });

  it('locks a partial retry to the original visit and stable chunk operations', async () => {
    const baseDraft: PassportAnonymousDraft = createDraft();
    const draft: PassportAnonymousDraft = {
      ...baseDraft,
      rides: [
        { ...baseDraft.rides[0], count: 100 },
        { ...baseDraft.rides[0], id: 'ride-2', count: 1 }
      ]
    };
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const compareAndSetDraft = vi.fn(async (): Promise<boolean> => true);
    const createVisitRequest = vi.fn(() => of(createVisit({ id: 'server-1' })));
    let importAttempt: number = 0;
    const importBatch = vi.fn((
      visitId: string,
      request: CreatePassportRideOccurrencesBatchRequest,
      _operationId: string
    ) => {
      importAttempt += 1;
      if (importAttempt === 2) {
        return throwError(() => new Error('second chunk unavailable'));
      }

      return of({
        occurrences: request.items.map((_: unknown, index: number): PassportRideOccurrence =>
          createOccurrence(`occurrence-${importAttempt}-${index}`, visitId)),
        wasReplayed: importAttempt === 3,
        wasOrderNormalized: false
      });
    });
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], deleteDraft, compareAndSetDraft),
      createVisitsPort({ createVisit: createVisitRequest }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);
    facade.setChoice(draft.id, 'Ignore');

    expect(facade.report()).toMatchObject({ failedCount: 1 });
    expect(facade.previews()[0].draft.pendingImport).toMatchObject({
      choice: 'Separate',
      targetVisitId: 'server-1'
    });
    expect(facade.previews()[0].decision.choice).toBe('Separate');

    await facade.importAll(true);

    expect(createVisitRequest).toHaveBeenCalledTimes(2);
    expect(importBatch).toHaveBeenCalledTimes(4);
    expect(importBatch.mock.calls[0][2]).toBe(importBatch.mock.calls[2][2]);
    expect(importBatch.mock.calls[1][2]).toBe(importBatch.mock.calls[3][2]);
    expect(compareAndSetDraft).toHaveBeenCalledTimes(2);
    expect(deleteDraft).toHaveBeenCalledWith(draft.id);
    expect(facade.report()).toMatchObject({ importedVisitCount: 1, failedCount: 0 });
    expect(facade.previews()).toEqual([]);
  });

  it('keeps the local draft when the visit acknowledgement changes the approximate-date flag', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const deleteDraft = vi.fn(async (): Promise<void> => undefined);
    const importBatch = vi.fn();
    const changedDate: PassportVisit = createVisit({
      date: { ...draft.visit.date, isApproximate: true }
    });
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], deleteDraft),
      createVisitsPort({ createVisit: () => of(changedDate) }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(importBatch).not.toHaveBeenCalled();
    expect(deleteDraft).not.toHaveBeenCalled();
    expect(facade.report()).toMatchObject({ failedCount: 1 });
    expect(facade.previews()).toHaveLength(1);
  });

  it('requires explicit consent before any local visit detail reaches the server', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const createVisitRequest = vi.fn(() => of(createVisit()));
    const listVisits = vi.fn(() => of({ items: [], nextCursor: null }));
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft]),
      createVisitsPort({ createVisit: createVisitRequest, listVisits }),
      createOccurrencesPort(),
      createParkItemsPort()
    );
    await facade.load();

    expect(listVisits).not.toHaveBeenCalled();
    await facade.prepareComparison(false);
    await facade.importAll(false);

    expect(listVisits).not.toHaveBeenCalled();
    expect(createVisitRequest).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.import.errors.consent');
  });

  it('aborts before any server mutation when another tab atomically claims the draft', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const compareAndSet = vi.fn(async (): Promise<boolean> => false);
    const createVisitRequest = vi.fn(() => of(createVisit()));
    const importBatch = vi.fn();
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], undefined, compareAndSet),
      createVisitsPort({ createVisit: createVisitRequest }),
      createOccurrencesPort({ importBatch }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(compareAndSet).toHaveBeenCalledTimes(1);
    expect(createVisitRequest).not.toHaveBeenCalled();
    expect(importBatch).not.toHaveBeenCalled();
    expect(facade.report()).toMatchObject({ failedCount: 1 });
    expect(facade.previews()).toHaveLength(1);
  });

  it('releases the local reservation after a definitive validation rejection', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const compareAndSet = vi.fn(async (): Promise<boolean> => true);
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], undefined, compareAndSet),
      createVisitsPort({
        createVisit: () => throwError(() => new HttpErrorResponse({ status: 422 }))
      }),
      createOccurrencesPort(),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(compareAndSet).toHaveBeenCalledTimes(2);
    expect(facade.previews()[0].draft.pendingImport).toBeNull();
    expect(facade.isImportLocked(facade.previews()[0])).toBe(false);
  });

  it('releases an untouched merge reservation when the selected visit disappeared', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const existing: PassportVisit = createVisit({ id: 'existing-1' });
    const compareAndSet = vi.fn(async (): Promise<boolean> => true);
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], undefined, compareAndSet),
      createVisitsPort({
        listVisits: () => of({ items: [existing], nextCursor: null }),
        getVisit: () => of(existing)
      }),
      createOccurrencesPort({
        importBatch: () => throwError(() => new HttpErrorResponse({ status: 404 }))
      }),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);
    facade.setChoice(draft.id, 'Merge');
    await facade.setTargetVisit(draft.id, existing.id);

    await facade.importAll(true);

    expect(compareAndSet).toHaveBeenCalledTimes(2);
    expect(facade.previews()[0].draft.pendingImport).toBeNull();
    expect(facade.isImportLocked(facade.previews()[0])).toBe(false);
  });

  it('keeps the local reservation after an ambiguous network failure', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const compareAndSet = vi.fn(async (): Promise<boolean> => true);
    const facade: PassportAnonymousImportStateFacade = new PassportAnonymousImportStateFacade(
      createStore([draft], undefined, compareAndSet),
      createVisitsPort({
        createVisit: () => throwError(() => new HttpErrorResponse({ status: 0 }))
      }),
      createOccurrencesPort(),
      createParkItemsPort()
    );
    await facade.load();
    await facade.prepareComparison(true);

    await facade.importAll(true);

    expect(compareAndSet).toHaveBeenCalledTimes(1);
    expect(facade.previews()[0].draft.pendingImport).toMatchObject({ choice: 'Separate' });
    expect(facade.isImportLocked(facade.previews()[0])).toBe(true);
  });
});

function createStore(
  drafts: PassportAnonymousDraft[],
  deleteDraft: (draftId: string) => Promise<void> = async (): Promise<void> => undefined,
  compareAndSetDraft: (
    expectedDraft: PassportAnonymousDraft,
    updatedDraft: PassportAnonymousDraft
  ) => Promise<boolean> = async (): Promise<boolean> => true
): PassportAnonymousDraftStorePort {
  return {
    isAvailable: (): boolean => true,
    list: async (): Promise<PassportAnonymousDraft[]> => drafts,
    get: async (): Promise<PassportAnonymousDraft | null> => drafts[0] ?? null,
    save: async (): Promise<void> => undefined,
    compareAndSet: compareAndSetDraft,
    deleteIfUnchanged: async (draft: PassportAnonymousDraft): Promise<boolean> => {
      await deleteDraft(draft.id);
      return true;
    },
    delete: deleteDraft,
    clear: async (): Promise<void> => undefined
  };
}

function createVisitsPort(
  overrides: Partial<PassportAnonymousImportVisitsPort> = {}
): PassportAnonymousImportVisitsPort {
  return {
    createVisit: () => of(createVisit()),
    getVisit: () => of(createVisit()),
    listVisits: () => of({ items: [], nextCursor: null }),
    updateVisit: () => of(createVisit()),
    ...overrides
  };
}

function createOccurrencesPort(
  overrides: Partial<PassportAnonymousImportOccurrencesPort> = {}
): PassportAnonymousImportOccurrencesPort {
  return {
    importBatch: () => of({
      occurrences: [
        createOccurrence('occurrence-1', 'server-1'),
        createOccurrence('occurrence-2', 'server-1')
      ],
      wasReplayed: false,
      wasOrderNormalized: false
    }),
    list: () => of({ items: [], nextCursor: null }),
    ...overrides
  };
}

function createParkItemsPort(
  overrides: Partial<PassportAnonymousImportParkItemsPort> = {}
): PassportAnonymousImportParkItemsPort {
  return {
    getParkItemsByParkIdPage: (parkId: string): ReturnType<PassportAnonymousImportParkItemsPort['getParkItemsByParkIdPage']> =>
      of<PagedResult<ParkItem>>({
        items: [{
          id: 'item-1',
          parkId,
          name: 'Attraction test',
          category: 'Attraction',
          type: 'Attraction',
          latitude: null,
          longitude: null
        }],
        pagination: {
          currentPage: 1,
          totalPages: 1,
          totalItems: 1,
          itemsPerPage: 100
        }
      }),
    ...overrides
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
      timeZoneId: 'Europe/Paris',
      serviceDayConvention: 'VisitStartLocalDate',
      title: null,
      privateNote: null
    },
    rides: [{
      id: 'ride-1',
      parkItemId: 'item-1',
      attractionName: 'Attraction test',
      moment: { localTime: '10:30', isApproximate: false },
      status: 'Completed',
      privateNote: 'Premier rang',
      confirmHistoricalConflict: false,
      count: 2
    }],
    createdAtUtc: '2026-09-04T10:00:00Z',
    updatedAtUtc: '2026-09-04T10:00:00Z'
  };
}

function createVisit(overrides: Partial<PassportVisit> = {}): PassportVisit {
  return {
    id: 'server-1',
    parkId: 'park-1',
    parkName: 'Parc test',
    date: { year: 2026, month: 9, day: 4, precision: 'Day', isApproximate: false },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-04T10:00:00Z',
    updatedAtUtc: '2026-09-04T10:00:00Z',
    completedAtUtc: null,
    ...overrides
  };
}

function createOccurrence(
  id: string,
  visitId: string,
  overrides: Partial<PassportRideOccurrence> = {}
): PassportRideOccurrence {
  return {
    id,
    visitId,
    parkId: 'park-1',
    parkItemId: 'item-1',
    sortPosition: 1000,
    moment: { localTime: '10:30:00', isApproximate: false },
    status: 'Completed',
    source: 'Import',
    historicalConsistency: 'Verified',
    privateNote: 'Premier rang',
    countsAsRide: true,
    version: 1,
    createdAtUtc: '2026-09-04T10:00:00Z',
    updatedAtUtc: '2026-09-04T10:00:00Z',
    ...overrides
  };
}
