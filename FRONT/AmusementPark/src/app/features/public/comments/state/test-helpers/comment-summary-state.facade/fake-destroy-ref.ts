import { DestroyRef } from '@angular/core';

export class FakeDestroyRef implements DestroyRef {
  readonly destroyed: boolean = false;

  onDestroy(_callback: () => void): () => void {
    return (): void => undefined;
  }
}
