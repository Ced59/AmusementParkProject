import { TestBed } from '@angular/core/testing';
import { ParamMap, Router, UrlTree, convertToParamMap, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { languageGuard } from './language.guard';

describe('languageGuard', () => {
  let translationService: jasmine.SpyObj<TranslationService>;
  let router: Router;

  beforeEach(() => {
    translationService = jasmine.createSpyObj<TranslationService>('TranslationService', ['isValidLang', 'useLang']);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: TranslationService, useValue: translationService }
      ]
    });
    router = TestBed.inject(Router);
  });

  async function runGuard(paramMap: ParamMap): Promise<boolean | UrlTree> {
    const result: unknown = TestBed.runInInjectionContext(() => languageGuard({ paramMap } as never, {} as never));
    return typeof result === 'boolean' || result instanceof UrlTree
      ? result
      : await firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('activates the route after switching to a supported language', async () => {
    translationService.isValidLang.and.returnValue(true);
    translationService.useLang.and.returnValue(of(null as never));

    await expectAsync(runGuard(convertToParamMap({ lang: 'fr' }))).toBeResolvedTo(true);
    expect(translationService.useLang).toHaveBeenCalledOnceWith('fr');
  });

  it('redirects unsupported language codes to English home', async () => {
    translationService.isValidLang.and.returnValue(false);

    const result: boolean | UrlTree = await runGuard(convertToParamMap({ lang: 'xx' }));

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/home');
    expect(translationService.useLang).not.toHaveBeenCalled();
  });

  it('redirects to English home when language activation fails', async () => {
    translationService.isValidLang.and.returnValue(true);
    translationService.useLang.and.returnValue(throwError(() => new Error('load failed')));

    const result: boolean | UrlTree = await runGuard(convertToParamMap({ lang: 'de' }));

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/home');
  });
});
