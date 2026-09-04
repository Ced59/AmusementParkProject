import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';
import { PassportOperationIdService } from '@data-access/passport/passport-operation-id.service';
import { PagedResult } from '@shared/models/contracts';
import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';
import { PASSPORT_ANONYMOUS_DRAFT_STORE_PORT, PassportAnonymousDraftStorePort } from './passport-anonymous-draft-store.ports';
import { PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT } from './passport-anonymous-draft-editor-data.ports';
import { PassportAnonymousDraftEditorStateFacade } from './passport-anonymous-draft-editor-state.facade';

describe('PassportAnonymousDraftEditorStateFacade', () => {
  it('persists a valid ride before reporting success', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const save = vi.fn(async (): Promise<void> => undefined);
    const facade: PassportAnonymousDraftEditorStateFacade = createFacade(draft, save);
    await facade.load(draft.id);

    const saved: boolean = await facade.addRide({
      parkItemId: 'item-1',
      attractionName: 'Attraction test',
      status: 'Completed',
      count: 2,
      localTime: '10:30',
      isApproximate: false,
      privateNote: 'Premier rang',
      confirmHistoricalConflict: false
    });

    expect(saved).toBe(true);
    expect(save).toHaveBeenCalledTimes(1);
    expect(facade.totalRideCount()).toBe(2);
  });

  it('keeps the current draft when local persistence fails', async () => {
    const draft: PassportAnonymousDraft = createDraft();
    const facade: PassportAnonymousDraftEditorStateFacade = createFacade(
      draft,
      vi.fn(async (): Promise<void> => { throw new Error('quota'); })
    );
    await facade.load(draft.id);

    const saved: boolean = await facade.addRide({
      parkItemId: 'item-1',
      attractionName: 'Attraction test',
      status: 'Completed',
      count: 1,
      localTime: '',
      isApproximate: false,
      privateNote: '',
      confirmHistoricalConflict: false
    });

    expect(saved).toBe(false);
    expect(facade.draft()?.rides).toEqual([]);
    expect(facade.errorKey()).toBe('passport.anonymousDrafts.editor.errors.save');
  });

  it('keeps only the latest attraction search response', async () => {
    const oldResponse = new Subject<PagedResult<ParkItem>>();
    const latestResponse = new Subject<PagedResult<ParkItem>>();
    const getParkItemsByParkIdPage = vi.fn()
      .mockReturnValueOnce(of(page([])))
      .mockReturnValueOnce(oldResponse)
      .mockReturnValueOnce(latestResponse);
    const facade: PassportAnonymousDraftEditorStateFacade = createFacade(
      createDraft(),
      vi.fn(async (): Promise<void> => undefined),
      getParkItemsByParkIdPage
    );
    await facade.load('draft-1');

    facade.searchAttractions('ancienne');
    facade.searchAttractions('nouvelle');
    latestResponse.next(page([parkItem('new-item', 'Nouvelle attraction')]));
    latestResponse.complete();
    oldResponse.next(page([parkItem('old-item', 'Ancienne attraction')]));
    oldResponse.complete();

    expect(getParkItemsByParkIdPage).toHaveBeenCalledTimes(3);
    expect(facade.attractions().map((item): string => item.id)).toEqual(['new-item']);
  });
});

function createFacade(
  draft: PassportAnonymousDraft,
  save: (value: PassportAnonymousDraft) => Promise<void>,
  getParkItemsByParkIdPage: ReturnType<typeof vi.fn> = vi.fn(() => of(page([])))
): PassportAnonymousDraftEditorStateFacade {
  const store: PassportAnonymousDraftStorePort = {
    isAvailable: (): boolean => true,
    list: async (): Promise<PassportAnonymousDraft[]> => [draft],
    get: async (): Promise<PassportAnonymousDraft | null> => draft,
    save,
    delete: async (): Promise<void> => undefined,
    clear: async (): Promise<void> => undefined
  };
  TestBed.configureTestingModule({
    providers: [
      PassportAnonymousDraftEditorStateFacade,
      { provide: PASSPORT_ANONYMOUS_DRAFT_STORE_PORT, useValue: store },
      { provide: PASSPORT_ANONYMOUS_DRAFT_ATTRACTIONS_PORT, useValue: { getParkItemsByParkIdPage } },
      { provide: PassportOperationIdService, useValue: { create: (): string => 'ride-operation-1' } }
    ]
  });
  return TestBed.inject(PassportAnonymousDraftEditorStateFacade);
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
    rides: [],
    createdAtUtc: '2026-09-04T10:00:00.000Z',
    updatedAtUtc: '2026-09-04T10:00:00.000Z'
  };
}

function page(items: ParkItem[]): PagedResult<ParkItem> {
  return {
    items,
    pagination: { totalItems: items.length, totalPages: 1, currentPage: 1, itemsPerPage: 20 }
  };
}

function parkItem(id: string, name: string): ParkItem {
  return {
    id,
    parkId: 'park-1',
    name,
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: null,
    longitude: null
  };
}
