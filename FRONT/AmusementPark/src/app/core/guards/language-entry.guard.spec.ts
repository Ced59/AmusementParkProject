import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';

import { LanguagePreferenceService } from '@app/services/localization/language-preference.service';
import { languageEntryGuard } from './language-entry.guard';

describe('languageEntryGuard', () => {
  it('keeps the neutral selector when no preference exists', () => {
    configure('browser', null);

    expect(runGuard()).toBe(true);
  });

  it('redirects a returning browser visitor to the preferred home', () => {
    const expectedTree = {} as UrlTree;
    const router = configure('browser', 'fr', expectedTree);

    expect(runGuard()).toBe(expectedTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/', 'fr', 'home']);
  });

  it('keeps the selector renderable on the server', () => {
    const router = configure('server', 'fr');

    expect(runGuard()).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });
});

function configure(platformId: string, language: string | null, urlTree: UrlTree = {} as UrlTree): {
  createUrlTree: ReturnType<typeof vi.fn>;
} {
  const router = {
    createUrlTree: vi.fn().mockReturnValue(urlTree),
  };
  TestBed.configureTestingModule({
    providers: [
      { provide: PLATFORM_ID, useValue: platformId },
      { provide: Router, useValue: router },
      {
        provide: LanguagePreferenceService,
        useValue: { getPreferredLanguage: (): string | null => language },
      },
    ],
  });
  return router;
}

function runGuard(): boolean | UrlTree {
  return TestBed.runInInjectionContext(() => languageEntryGuard(
    {} as ActivatedRouteSnapshot,
    {} as RouterStateSnapshot
  )) as boolean | UrlTree;
}
