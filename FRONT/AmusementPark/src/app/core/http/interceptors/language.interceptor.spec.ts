import { HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { LanguageInterceptor } from './language.interceptor';

describe('LanguageInterceptor', () => {
  function createHandler(assertRequest: (request: HttpRequest<unknown>) => void): HttpHandler {
    return {
      handle: (request: HttpRequest<unknown>) => {
        assertRequest(request);
        return of(new HttpResponse({ status: 200 }));
      }
    };
  }

  it('adds the current language as Accept-Language header', (done) => {
    const translationService = jasmine.createSpyObj<TranslationService>('TranslationService', ['getCurrentLangCode']);
    translationService.getCurrentLangCode.and.returnValue('fr-FR');
    const interceptor = new LanguageInterceptor(translationService);

    interceptor.intercept(new HttpRequest('GET', '/api/parks'), createHandler((request: HttpRequest<unknown>) => {
      expect(request.headers.get('Accept-Language')).toBe('fr-FR');
    })).subscribe(() => done());
  });

  it('falls back to en-US when the translation service returns an empty language', (done) => {
    const translationService = jasmine.createSpyObj<TranslationService>('TranslationService', ['getCurrentLangCode']);
    translationService.getCurrentLangCode.and.returnValue('');
    const interceptor = new LanguageInterceptor(translationService);

    interceptor.intercept(new HttpRequest('GET', '/api/parks'), createHandler((request: HttpRequest<unknown>) => {
      expect(request.headers.get('Accept-Language')).toBe('en-US');
    })).subscribe(() => done());
  });
});
