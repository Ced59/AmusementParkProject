import { DestroyRef } from '@angular/core';
import { of, throwError } from 'rxjs';

import { TripPassportTransition } from '@app/models/trips/trip-passport-transition.models';
import { TripPassportTransitionDataPort } from './trip-passport-transition-data.port';
import { TripPassportTransitionFacade } from './trip-passport-transition.facade';

describe('TripPassportTransitionFacade', () => {
  it('loads the proposal and confirms only selectable unique attraction ids', () => {
    const transition: TripPassportTransition = createTransition();
    const data: TripPassportTransitionDataPort = {
      get: vi.fn().mockReturnValue(of(transition)),
      confirm: vi.fn().mockReturnValue(of({
        visitId: 'visit-1',
        wasReplayed: false,
        addedRideCount: 1
      }))
    };
    const facade: TripPassportTransitionFacade = createFacade(data);

    facade.load(' trip-1 ');
    facade.confirm('2027-08-20', ['item-1', 'item-1']);

    expect(data.get).toHaveBeenCalledWith('trip-1');
    expect(data.confirm).toHaveBeenCalledWith('trip-1', '2027-08-20', {
      parkItemIds: ['item-1']
    });
    expect(facade.confirmation()?.visitId).toBe('visit-1');
    expect(facade.transition()?.days[0].existingVisitId).toBe('visit-1');
    expect(facade.transition()?.days[0].canConfirm).toBe(false);
    expect(facade.transition()?.days[0].canResume).toBe(false);
  });

  it('never submits an attraction that does not belong to the proposed day', () => {
    const data: TripPassportTransitionDataPort = {
      get: vi.fn().mockReturnValue(of(createTransition())),
      confirm: vi.fn().mockReturnValue(of({ visitId: 'visit-1' }))
    };
    const facade: TripPassportTransitionFacade = createFacade(data);

    facade.load('trip-1');
    facade.confirm('2027-08-20', ['foreign-item']);

    expect(data.confirm).not.toHaveBeenCalled();
  });

  it('keeps the proposal visible when confirmation fails', () => {
    const transition: TripPassportTransition = createTransition();
    const data: TripPassportTransitionDataPort = {
      get: vi.fn().mockReturnValue(of(transition)),
      confirm: vi.fn().mockReturnValue(throwError(() => new Error('offline')))
    };
    const facade: TripPassportTransitionFacade = createFacade(data);

    facade.load('trip-1');
    facade.confirm('2027-08-20', []);

    expect(facade.transition()).toEqual(transition);
    expect(facade.feedbackKey()).toBe('trips.passportTransition.feedback.confirmError');
  });
});

function createFacade(data: TripPassportTransitionDataPort): TripPassportTransitionFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripPassportTransitionFacade(data, destroyRef);
}

function createTransition(): TripPassportTransition {
  return {
    title: 'Voyage test',
    destinationToday: '2027-08-22',
    days: [{
      localDate: '2027-08-20',
      parkId: 'park-1',
      parkName: 'Parc test',
      isParkAvailable: true,
      canConfirm: true,
      canResume: false,
      existingVisitId: null,
      existingVisitStatus: null,
      attractions: [{
        parkItemId: 'item-1',
        name: 'Grand huit',
        mainImageId: 'image-1',
        ownPreference: 'MustDo',
        historicalConsistency: 'Verified'
      }]
    }]
  };
}
