import { DestroyRef } from '@angular/core';

export class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}
