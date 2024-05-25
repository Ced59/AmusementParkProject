import {HttpEvent, HttpHandler, HttpInterceptor, HttpInterceptorFn, HttpRequest} from '@angular/common/http';
import {Injectable} from "@angular/core";
import {TranslationService} from "../services/translation.service";
import {Observable} from "rxjs";

@Injectable()
export class LanguageInterceptor implements HttpInterceptor {
  constructor(private translationService: TranslationService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const currentLang = this.translationService.getCurrentLangCode() || 'en-US';
    console.log(currentLang);

    const modifiedReq = req.clone({
      headers: req.headers.set('Accept-Language', currentLang)
    });

    return next.handle(modifiedReq);
  }
}
