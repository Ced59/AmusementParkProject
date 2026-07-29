import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  ParamMap,
  Router,
  UrlTree,
  convertToParamMap,
  provideRouter,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { languageGuard } from './language.guard';

describe('languageGuard', () => {
  let translationService: MockedObject<TranslationService>;
  let router: Router;

  beforeEach(() => {
    translationService = {
      isValidLang: vi.fn().mockName('TranslationService.isValidLang'),
      useLang: vi.fn().mockName('TranslationService.useLang'),
    } as unknown as MockedObject<TranslationService>;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: TranslationService, useValue: translationService },
      ],
    });
    router = TestBed.inject(Router);
  });

  async function runGuard(paramMap: ParamMap): Promise<boolean | UrlTree> {
    const result: unknown = TestBed.runInInjectionContext(() =>
      languageGuard({ paramMap } as never, {} as never),
    );
    return typeof result === 'boolean' || result instanceof UrlTree
      ? result
      : await firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('activates the route after switching to a supported language', async () => {
    translationService.isValidLang.mockReturnValue(true);
    translationService.useLang.mockReturnValue(of(null as never));

    await expect(runGuard(convertToParamMap({ lang: 'fr' }))).resolves.toEqual(
      true,
    );
    expect(translationService.useLang).toHaveBeenCalledTimes(1);
    expect(translationService.useLang).toHaveBeenCalledWith('fr');
  });

  it('redirects unsupported language codes to English home', async () => {
    translationService.isValidLang.mockReturnValue(false);

    const result: boolean | UrlTree = await runGuard(
      convertToParamMap({ lang: 'xx' }),
    );

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/home');
    expect(translationService.useLang).not.toHaveBeenCalled();
  });

  it('redirects malformed language segments to English not found', async () => {
    translationService.isValidLang.mockReturnValue(false);

    const result: boolean | UrlTree = await runGuard(
      convertToParamMap({ lang: 'route-inconnue' }),
    );

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/not-found');
    expect(translationService.useLang).not.toHaveBeenCalled();
  });

  it('redirects to English home when language activation fails', async () => {
    translationService.isValidLang.mockReturnValue(true);
    translationService.useLang.mockReturnValue(
      throwError(() => new Error('load failed')),
    );

    const result: boolean | UrlTree = await runGuard(
      convertToParamMap({ lang: 'de' }),
    );

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/home');
  });
});
