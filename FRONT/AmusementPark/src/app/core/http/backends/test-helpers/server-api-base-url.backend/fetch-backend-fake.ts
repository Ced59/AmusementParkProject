import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';

import { defer, Observable, of } from 'rxjs';

export class FetchBackendFake {
  public capturedUrl: string = '';
  public capturedInternalSsrHeader: string | null = null;
  public subscriptionCount: number = 0;

  constructor(
    private readonly responseFactory: (
      attempt: number,
      request: HttpRequest<unknown>,
    ) => Observable<HttpEvent<unknown>> = () =>
      of(new HttpResponse({ status: 200 })),
  ) {}

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    this.capturedUrl = request.url;
    this.capturedInternalSsrHeader = request.headers.get(
      'X-AmusementPark-Internal-SSR',
    );
    return defer(() => {
      this.subscriptionCount += 1;
      return this.responseFactory(this.subscriptionCount, request);
    });
  }
}
