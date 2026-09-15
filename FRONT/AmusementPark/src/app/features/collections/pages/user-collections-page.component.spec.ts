import { DestroyRef, Signal } from '@angular/core';
import { Subject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { UserCollectionsPageFacade } from '../state/user-collections-page.facade';
import { UserCollectionsPageComponent } from './user-collections-page.component';

interface UserCollectionsPageTestSurface {
  currentLang: Signal<string>;
}

describe('UserCollectionsPageComponent', () => {
  it('keeps collection links synchronized with the active language', () => {
    const languageChanged: Subject<string> = new Subject<string>();
    const destroyRef: DestroyRef = {
      destroyed: false,
      onDestroy: (): (() => void) => (): void => undefined
    };
    const component: UserCollectionsPageComponent = new UserCollectionsPageComponent(
      {} as UserCollectionsPageFacade,
      {
        getCurrentLang: (): string => 'en',
        languageChanged
      } as unknown as TranslationService,
      destroyRef
    );
    const page: UserCollectionsPageTestSurface =
      component as unknown as UserCollectionsPageTestSurface;

    expect(page.currentLang()).toBe('en');

    languageChanged.next('fr');

    expect(page.currentLang()).toBe('fr');
  });
});
