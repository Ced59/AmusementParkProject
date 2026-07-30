import { isPlatformBrowser } from '@angular/common';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { Observable } from 'rxjs';
import { retry } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import {
  resolveTransientHttpReadRetryDelay,
  TRANSIENT_HTTP_READ_RETRY_COUNT
} from '../transient-http-read-retry.policy';

@Injectable()
export class TransientApiReadRetryInterceptor implements HttpInterceptor {
  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {
  }

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!isPlatformBrowser(this.platformId) || !this.isApiRequest(request.url)) {
      return next.handle(request);
    }

    return next.handle(request).pipe(
      retry({
        count: TRANSIENT_HTTP_READ_RETRY_COUNT,
        delay: (error: unknown, retryCount: number) => {
          return resolveTransientHttpReadRetryDelay(error, request, retryCount);
        }
      })
    );
  }

  private isApiRequest(url: string): boolean {
    const apiBaseUrl: string = TransientApiReadRetryInterceptor.ensureTrailingSlash(environment.apiBaseUrl);
    return url.startsWith(apiBaseUrl);
  }

  private static ensureTrailingSlash(value: string): string {
    return value.endsWith('/') ? value : `${value}/`;
  }
}
