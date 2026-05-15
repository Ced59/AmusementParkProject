import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';

@Injectable()
export class LanguageInterceptor implements HttpInterceptor {
  constructor(private readonly translationService: TranslationService) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const currentLang: string = this.translationService.getCurrentLangCode() || 'en-US';
    const modifiedReq: HttpRequest<unknown> = req.clone({
      headers: req.headers.set('Accept-Language', currentLang)
    });

    return next.handle(modifiedReq);
  }
}
