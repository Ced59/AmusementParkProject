import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import { TripPreferenceBoard } from '@app/models/trips/trip.models';
import { TripPreferencesDataPort } from './trip-preferences-data.port';
import { TripPreferencesFacade } from './trip-preferences.facade';

describe('TripPreferencesFacade', () => {
  it('keeps drafts local and saves several choices as one batch', () => {
    const board: TripPreferenceBoard = createBoard();
    const updated: TripPreferenceBoard = createBoard();
    const data: TripPreferencesDataPort = {
      getMine: vi.fn().mockReturnValue(of(board)),
      set: vi.fn().mockReturnValue(of(updated)),
      setBatch: vi.fn().mockReturnValue(of(updated))
    };
    const facade: TripPreferencesFacade = createFacade(data);

    facade.load('trip-1');
    facade.setLevel('item-1', 'MustDo');
    facade.setLevel('item-2', 'NotForMe');

    expect(facade.pendingCount()).toBe(2);
    expect(facade.visibleItems().map((item) => item.level)).toEqual(['MustDo', 'NotForMe']);

    facade.save();

    expect(data.setBatch).toHaveBeenCalledWith('trip-1', {
      expectedPlanVersion: 7,
      preferences: [
        {
          parkItemId: 'item-1',
          expectedPreferenceVersion: null,
          level: 'MustDo',
          reason: null
        },
        {
          parkItemId: 'item-2',
          expectedPreferenceVersion: 3,
          level: 'NotForMe',
          reason: null
        }
      ]
    });
    expect(data.set).not.toHaveBeenCalled();
    expect(facade.pendingCount()).toBe(0);
  });

  it('uses the single-item endpoint and removes reasons when returning to no opinion', () => {
    const board: TripPreferenceBoard = createBoard();
    const data: TripPreferencesDataPort = {
      getMine: vi.fn().mockReturnValue(of(board)),
      set: vi.fn().mockReturnValue(of(board)),
      setBatch: vi.fn().mockReturnValue(of(board))
    };
    const facade: TripPreferencesFacade = createFacade(data);

    facade.load('trip-1');
    facade.setReason('item-2', 'Height');
    facade.setLevel('item-2', 'Unknown');
    facade.save();

    expect(data.set).toHaveBeenCalledWith('trip-1', 'item-2', {
      expectedPlanVersion: 7,
      expectedPreferenceVersion: 3,
      level: 'Unknown',
      reason: null
    });
    expect(data.setBatch).not.toHaveBeenCalled();
  });

  it('does not expose editing drafts to viewers', () => {
    const data: TripPreferencesDataPort = {
      getMine: vi.fn().mockReturnValue(of(createBoard(false))),
      set: vi.fn().mockReturnValue(of(createBoard(false))),
      setBatch: vi.fn().mockReturnValue(of(createBoard(false)))
    };
    const facade: TripPreferencesFacade = createFacade(data);

    facade.load('trip-1');
    facade.setLevel('item-1', 'MustDo');
    facade.save();

    expect(facade.pendingCount()).toBe(0);
    expect(data.set).not.toHaveBeenCalled();
    expect(data.setBatch).not.toHaveBeenCalled();
  });

  it('serializes very large saves into server-sized batches', () => {
    const board: TripPreferenceBoard = createLargeBoard(251);
    const data: TripPreferencesDataPort = {
      getMine: vi.fn().mockReturnValue(of(board)),
      set: vi.fn().mockReturnValue(of(board)),
      setBatch: vi.fn().mockReturnValue(of(board))
    };
    const facade: TripPreferencesFacade = createFacade(data);

    facade.load('trip-1');
    for (const item of board.items) {
      facade.setLevel(item.parkItemId, 'MustDo');
    }
    facade.save();

    expect(data.setBatch).toHaveBeenCalledTimes(2);
    const calls = vi.mocked(data.setBatch).mock.calls;
    expect(calls[0][1].preferences).toHaveLength(250);
    expect(calls[1][1].preferences).toHaveLength(1);
    expect(facade.pendingCount()).toBe(0);
  });
});

function createFacade(data: TripPreferencesDataPort): TripPreferencesFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripPreferencesFacade(data, destroyRef);
}

function createBoard(canVote: boolean = true): TripPreferenceBoard {
  return {
    tripPlanId: 'trip-1',
    tripTitle: 'Voyage test',
    planVersion: 7,
    canVote,
    items: [
      {
        parkId: 'park-1',
        parkName: 'Parc test',
        parkItemId: 'item-1',
        parkItemName: 'Grand huit',
        mainImageId: null,
        level: 'Unknown',
        reason: null,
        version: null
      },
      {
        parkId: 'park-1',
        parkName: 'Parc test',
        parkItemId: 'item-2',
        parkItemName: 'Tour',
        mainImageId: 'image-2',
        level: 'WantToDo',
        reason: null,
        version: 3
      }
    ]
  };
}

function createLargeBoard(itemCount: number): TripPreferenceBoard {
  return {
    tripPlanId: 'trip-1',
    tripTitle: 'Grand voyage test',
    planVersion: 7,
    canVote: true,
    items: Array.from({ length: itemCount }, (_, index: number) => ({
      parkId: `park-${Math.floor(index / 20)}`,
      parkName: `Parc ${Math.floor(index / 20)}`,
      parkItemId: `item-${index}`,
      parkItemName: `Attraction ${index}`,
      mainImageId: null,
      level: 'Unknown' as const,
      reason: null,
      version: null
    }))
  };
}
