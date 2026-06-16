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
    const translateService: jasmine.SpyObj<TranslateService> = jasmine.createSpyObj<TranslateService>('TranslateService', [
      'setDefaultLang',
      'use'
    ]);
    const testDocument: Document = createDocumentForPath('/fr/parcs/phantasialand');
    const testedService = new TranslationService(translateService, testDocument);
    translateService.use.and.returnValue(of({}));

    await testedService.initializeLanguage();

    expect(translateService.setDefaultLang).toHaveBeenCalledOnceWith('fr');
    expect(translateService.use).toHaveBeenCalledOnceWith('fr');
  });

  it('loads English only as fallback when the requested language fails', async () => {
    spyOn(console, 'error');
    const translateService: jasmine.SpyObj<TranslateService> = jasmine.createSpyObj<TranslateService>('TranslateService', [
      'setDefaultLang',
      'use'
    ]);
    const testDocument: Document = createDocumentForPath('/fr/parcs/phantasialand');
    const testedService = new TranslationService(translateService, testDocument);
    translateService.use.withArgs('fr').and.returnValue(throwError(() => new Error('network')));
    translateService.use.withArgs('en').and.returnValue(of({}));

    await testedService.initializeLanguage();

    expect(translateService.setDefaultLang.calls.allArgs()).toEqual([['fr'], ['en']]);
    expect(translateService.use.calls.allArgs()).toEqual([['fr'], ['en']]);
  });
});

function createDocumentForPath(pathname: string): Document {
  return {
    location: { pathname },
    documentElement: {
      lang: '',
      getAttribute: () => 'en'
    }
  } as unknown as Document;
}
