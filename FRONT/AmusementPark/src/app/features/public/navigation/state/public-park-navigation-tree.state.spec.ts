import { TestBed } from '@angular/core/testing';

import { PublicParkNavigationTreeItem } from '../models/public-park-navigation-tree.model';
import { PublicParkNavigationTreeState } from './public-park-navigation-tree.state';

describe('PublicParkNavigationTreeState', (): void => {
  let state: PublicParkNavigationTreeState;

  beforeEach((): void => {
    TestBed.configureTestingModule({
      providers: [PublicParkNavigationTreeState]
    });
    state = TestBed.inject(PublicParkNavigationTreeState);
  });

  it('keeps existing items visible while a new load is running', (): void => {
    const firstLoadId: number = state.beginLoad();
    const item: PublicParkNavigationTreeItem = {
      id: 'park',
      label: 'Park',
      icon: 'map',
      routeCommands: ['/fr/park/id/slug'],
      level: 0,
      isCurrent: true,
    };
    state.completeLoad(firstLoadId, [item]);

    state.beginLoad();

    expect(state.tree()).toEqual({
      isAvailable: true,
      isLoading: true,
      items: [item]
    });
  });

  it('ignores a response superseded by a newer navigation', (): void => {
    const staleLoadId: number = state.beginLoad();
    const currentLoadId: number = state.beginLoad();

    state.completeLoad(staleLoadId, []);

    expect(state.tree().isLoading).toBeTrue();
    expect(state.isCurrent(currentLoadId)).toBeTrue();
  });

  it('clears the tree when the route is outside a park context', (): void => {
    state.beginLoad();
    state.markUnavailable();

    expect(state.tree()).toEqual({
      isAvailable: false,
      isLoading: false,
      items: []
    });
  });
});
