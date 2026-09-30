import { TestBed } from '@angular/core/testing';

import { PassportAnonymousImportStateFacade } from '../../state/passport-anonymous-import-state.facade';
import { PassportAnonymousImportPanelComponent } from './passport-anonymous-import-panel.component';

describe('PassportAnonymousImportPanelComponent', () => {
  it('hides technical identifiers used as unresolved display names', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: PassportAnonymousImportStateFacade, useValue: {} }
      ]
    });
    const component: PassportAnonymousImportPanelComponent = TestBed.runInInjectionContext(
      (): PassportAnonymousImportPanelComponent => new PassportAnonymousImportPanelComponent()
    );
    const labels = component as unknown as {
      displayParkName(parkName: string | null, parkId: string): string | null;
      displayRideName(attractionName: string | null, parkItemId?: string): string | null;
    };

    expect(labels.displayParkName('park-technical-id', 'park-technical-id')).toBeNull();
    expect(labels.displayParkName(' Parc Astérix ', 'park-technical-id')).toBe('Parc Astérix');
    expect(labels.displayRideName('item-technical-id', 'item-technical-id')).toBeNull();
    expect(labels.displayRideName(' OzIris ', 'item-technical-id')).toBe('OzIris');
  });
});
