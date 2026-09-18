import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import { TripActivityPage } from '@app/models/trips/trip-activity.models';
import { TripActivityDataPort } from './trip-activity-data.port';
import { TripActivityFacade } from './trip-activity.facade';

describe('TripActivityFacade', () => {
  it('replaces on refresh and appends older pages without duplicate sequences', () => {
    const first: TripActivityPage = createPage([3, 2], 2);
    const older: TripActivityPage = createPage([2, 1], null);
    const refreshed: TripActivityPage = createPage([4, 3], 3);
    const data: TripActivityDataPort = {
      get: vi.fn()
        .mockReturnValueOnce(of(first))
        .mockReturnValueOnce(of(older))
        .mockReturnValueOnce(of(refreshed))
    };
    const facade: TripActivityFacade = createFacade(data);

    facade.load(' trip-1 ');
    facade.loadMore();
    expect(facade.entries().map((entry) => entry.sequence)).toEqual([3, 2, 1]);

    facade.refresh();
    expect(facade.entries().map((entry) => entry.sequence)).toEqual([4, 3]);
    expect(data.get).toHaveBeenNthCalledWith(1, 'trip-1');
    expect(data.get).toHaveBeenNthCalledWith(2, 'trip-1', 2);
  });

  it('ignores an empty trip identifier', () => {
    const data: TripActivityDataPort = { get: vi.fn().mockReturnValue(of(createPage([], null))) };
    const facade: TripActivityFacade = createFacade(data);

    facade.load('   ');

    expect(data.get).not.toHaveBeenCalled();
  });
});

function createFacade(data: TripActivityDataPort): TripActivityFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripActivityFacade(data, destroyRef);
}

function createPage(sequences: number[], nextBeforeSequence: number | null): TripActivityPage {
  return {
    tripTitle: 'Voyage test',
    entries: sequences.map((sequence: number) => ({
      sequence,
      kind: 'TripRenamed' as const,
      actorDisplayName: 'Camille',
      isCurrentUser: false,
      affectedCount: 1,
      occurredAtUtc: '2026-09-18T12:00:00Z'
    })),
    nextBeforeSequence
  };
}
