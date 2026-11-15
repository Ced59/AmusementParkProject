import { HttpContext } from '@angular/common/http';

import { SKIP_AUTHORIZATION_HEADER } from './auth-request-policy';

export interface AnonymousHttpOptions {
  readonly context: HttpContext;
  readonly includeItems?: boolean;
}

export function anonymousHttpOptions(): AnonymousHttpOptions {
  return {
    context: new HttpContext().set(SKIP_AUTHORIZATION_HEADER, true)
  };
}
