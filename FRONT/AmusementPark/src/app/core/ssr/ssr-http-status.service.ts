import { Inject, Injectable, Optional } from '@angular/core';

import { SSR_RESPONSE, SsrResponseLike } from './ssr-response.token';

@Injectable({ providedIn: 'root' })
export class SsrHttpStatusService {
  constructor(@Optional() @Inject(SSR_RESPONSE) private readonly response: SsrResponseLike | null) {
  }

  setNotFound(): void {
    this.setStatus(404);
  }

  setStatus(statusCode: number): void {
    if (!this.response) {
      return;
    }

    this.response.status(statusCode);
  }
}
