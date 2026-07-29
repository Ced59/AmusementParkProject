import { HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { firstValueFrom, of } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { LanguageInterceptor } from './language.interceptor';

describe('LanguageInterceptor', () => {
  function createHandler(
    assertRequest: (request: HttpRequest<unknown>) => void,
  ): HttpHandler {
    return {
      handle: (request: HttpRequest<unknown>) => {
        assertRequest(request);
        return of(new HttpResponse({ status: 200 }));
      },
    };
  }

  it('adds the current language as Accept-Language header', async () => {
    const translationService = {
      getCurrentLangCode: vi
        .fn()
        .mockName('TranslationService.getCurrentLangCode'),
    };
    translationService.getCurrentLangCode.mockReturnValue('fr-FR');
    const interceptor = new LanguageInterceptor(
      translationService as unknown as TranslationService,
    );

    await firstValueFrom(
      interceptor.intercept(
        new HttpRequest('GET', '/api/parks'),
        createHandler((request: HttpRequest<unknown>) => {
          expect(request.headers.get('Accept-Language')).toBe('fr-FR');
        }),
      ),
    );
  });

  it('falls back to en-US when the translation service returns an empty language', async () => {
    const translationService = {
      getCurrentLangCode: vi
        .fn()
        .mockName('TranslationService.getCurrentLangCode'),
    };
    translationService.getCurrentLangCode.mockReturnValue('');
    const interceptor = new LanguageInterceptor(
      translationService as unknown as TranslationService,
    );

    await firstValueFrom(
      interceptor.intercept(
        new HttpRequest('GET', '/api/parks'),
        createHandler((request: HttpRequest<unknown>) => {
          expect(request.headers.get('Accept-Language')).toBe('en-US');
        }),
      ),
    );
  });
});
