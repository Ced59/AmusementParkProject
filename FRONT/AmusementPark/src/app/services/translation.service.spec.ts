import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { PublicFirstTranslateLoader } from '@core/i18n/public-first-translate.loader';

import { TranslationService } from './translation.service';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('TranslationService', () => {
  let service: TranslationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(TranslationService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('uses the route language as default language during initialization', async () => {
    const translateService: MockedObject<TranslateService> = {
      setDefaultLang: vi.fn().mockName('TranslateService.setDefaultLang'),
      use: vi.fn().mockName('TranslateService.use'),
    } as unknown as MockedObject<TranslateService>;
    const testDocument: Document = createDocumentForPath(
      '/fr/parcs/phantasialand',
    );
    const testedService = new TranslationService(
      translateService,
      testDocument,
    );
    translateService.use.mockReturnValue(of({}));

    await testedService.initializeLanguage();

    expect(translateService.setDefaultLang).toHaveBeenCalledTimes(1);

    expect(translateService.setDefaultLang).toHaveBeenCalledWith('fr');
    expect(translateService.use).toHaveBeenCalledTimes(1);
    expect(translateService.use).toHaveBeenCalledWith('fr');
  });

  it('loads English only as fallback when the requested language fails', async () => {
    vi.spyOn(console, 'error');
    const translateService: MockedObject<TranslateService> = {
      setDefaultLang: vi.fn().mockName('TranslateService.setDefaultLang'),
      use: vi.fn().mockName('TranslateService.use'),
    } as unknown as MockedObject<TranslateService>;
    const testDocument: Document = createDocumentForPath(
      '/fr/parcs/phantasialand',
    );
    const testedService = new TranslationService(
      translateService,
      testDocument,
    );
    translateService.use.mockImplementation((language: string) =>
      language === 'fr'
        ? throwError(() => new Error('network'))
        : of({}),
    );

    await testedService.initializeLanguage();

    expect(vi.mocked(translateService.setDefaultLang).mock.calls).toEqual([
      ['fr'],
      ['en'],
    ]);
    expect(vi.mocked(translateService.use).mock.calls).toEqual([
      ['fr'],
      ['en'],
    ]);
  });

  it('adds complete translations before reusing a language cached by ngx-translate', async () => {
    const loader = new PublicFirstTranslateLoader({} as HttpClient);
    vi.spyOn(loader, 'requiresCompleteTranslations', 'get').mockReturnValue(true);
    const dictionary = { home: { title: 'Start' }, admin: { title: 'Verwaltung' } };
    const load = vi.spyOn(loader, 'getCompleteTranslation').mockReturnValue(of(dictionary));
    const translate = {
      currentLang: 'fr',
      setTranslation: vi.fn(),
      use: vi.fn().mockReturnValue(of(dictionary))
    } as unknown as MockedObject<TranslateService>;
    const testedService = new TranslationService(translate, createDocumentForPath('/fr/admin'), loader);

    await firstValueFrom(testedService.useLang('de'));

    expect(load).toHaveBeenCalledWith('de');
    expect(translate.setTranslation).toHaveBeenCalledWith('de', dictionary, true);
    expect(translate.setTranslation.mock.invocationCallOrder[0]).toBeLessThan(translate.use.mock.invocationCallOrder[0]);
    expect(translate.use).toHaveBeenCalledWith('de');
  });

  it('keeps complete server or standard loaders unchanged', async () => {
    const loader = { getTranslation: vi.fn() } as TranslateLoader;
    const translate = { currentLang: 'fr', setTranslation: vi.fn() } as unknown as MockedObject<TranslateService>;
    const testedService = new TranslationService(translate, createDocumentForPath('/fr/admin'), loader);

    await firstValueFrom(testedService.loadCompleteTranslations('fr'));

    expect(loader.getTranslation).not.toHaveBeenCalled();
    expect(translate.setTranslation).not.toHaveBeenCalled();
  });
});

function createDocumentForPath(pathname: string): Document {
  return {
    location: { pathname },
    documentElement: {
      lang: '',
      getAttribute: () => 'en',
    },
  } as unknown as Document;
}
