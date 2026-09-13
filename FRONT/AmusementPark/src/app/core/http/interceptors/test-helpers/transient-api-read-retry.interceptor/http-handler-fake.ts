import { HttpEvent, HttpHandler, HttpRequest } from '@angular/common/http';

import { defer, Observable } from 'rxjs';

export class HttpHandlerFake implements HttpHandler {
  public subscriptionCount: number = 0;

  constructor(
    private readonly responseFactory: (
      attempt: number,
      request: HttpRequest<unknown>
    ) => Observable<HttpEvent<unknown>>
  ) {
  }

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    return defer(() => {
      this.subscriptionCount += 1;
      return this.responseFactory(this.subscriptionCount, request);
    });
  }
}
