import { DestroyRef, Signal } from '@angular/core';
import { Subject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { ParkFitGroupProfileManagementFacade } from '../state/park-fit-group-profile-management.facade';
import { ParkFitGroupProfilesPageComponent } from './park-fit-group-profiles-page.component';

interface ParkFitGroupProfilesPageTestSurface {
  currentLang: Signal<string>;
}

describe('ParkFitGroupProfilesPageComponent', () => {
  it('keeps its Park Fit back link synchronized with the active language', () => {
    const languageChanged: Subject<string> = new Subject<string>();
    const destroyRef: DestroyRef = {
      destroyed: false,
      onDestroy: (): (() => void) => (): void => undefined
    };
    const component: ParkFitGroupProfilesPageComponent =
      new ParkFitGroupProfilesPageComponent(
        {} as ParkFitGroupProfileManagementFacade,
        {
          getCurrentLang: (): string => 'fr',
          languageChanged
        } as unknown as TranslationService,
        destroyRef
      );
    const page: ParkFitGroupProfilesPageTestSurface =
      component as unknown as ParkFitGroupProfilesPageTestSurface;

    expect(page.currentLang()).toBe('fr');

    languageChanged.next('de');

    expect(page.currentLang()).toBe('de');
  });
});
