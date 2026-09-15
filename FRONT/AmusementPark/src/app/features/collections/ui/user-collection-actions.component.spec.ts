import { UserCollectionKind } from '@app/models/watchlists/user-collection-entry.model';
import { UserCollectionActionsFacade } from '../state/user-collection-actions.facade';
import { UserCollectionActionsComponent } from './user-collection-actions.component';

interface UserCollectionActionsTestSurface {
  labelKey(kind: UserCollectionKind): string;
  icon(kind: UserCollectionKind): string;
}

describe('UserCollectionActionsComponent', () => {
  it('presents a distinct action for adding a target to plans', () => {
    const facade: Pick<UserCollectionActionsFacade, 'has'> = {
      has: vi.fn().mockReturnValue(false)
    };
    const component: UserCollectionActionsComponent = new UserCollectionActionsComponent(
      facade as UserCollectionActionsFacade
    );
    const surface: UserCollectionActionsTestSurface =
      component as unknown as UserCollectionActionsTestSurface;

    expect(surface.labelKey('Planned')).toBe('collections.actions.planned');
    expect(surface.icon('Planned')).toBe('pi pi-calendar-plus');
  });

  it('identifies an already planned target', () => {
    const facade: Pick<UserCollectionActionsFacade, 'has'> = {
      has: vi.fn((kind: UserCollectionKind): boolean => kind === 'Planned')
    };
    const component: UserCollectionActionsComponent = new UserCollectionActionsComponent(
      facade as UserCollectionActionsFacade
    );
    const surface: UserCollectionActionsTestSurface =
      component as unknown as UserCollectionActionsTestSurface;

    expect(surface.labelKey('Planned')).toBe('collections.actions.plannedActive');
    expect(surface.icon('Planned')).toBe('pi pi-calendar');
  });
});
