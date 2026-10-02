import { isPlatformBrowser, isPlatformServer } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, TransferState, makeStateKey } from '@angular/core';

/** Transfers mapped public view data instead of complete API responses. */
@Injectable({ providedIn: 'root' })
export class PublicViewTransferState {
  constructor(
    private readonly transferState: TransferState,
    @Inject(PLATFORM_ID) private readonly platformId: string
  ) {
  }

  consume<T>(name: string): T | undefined {
    const key = makeStateKey<T | undefined>(name);
    const value: T | undefined = this.transferState.get(key, undefined);
    if (isPlatformBrowser(this.platformId)) {
      this.transferState.remove(key);
    }
    return value;
  }

  store<T>(name: string, value: T): void {
    if (isPlatformServer(this.platformId)) {
      this.transferState.set(makeStateKey<T>(name), value);
    }
  }
}
