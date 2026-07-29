import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

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
