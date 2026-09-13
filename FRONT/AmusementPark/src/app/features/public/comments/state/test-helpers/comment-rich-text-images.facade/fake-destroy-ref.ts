import { DestroyRef } from '@angular/core';

export class FakeDestroyRef implements DestroyRef {
  destroyed: boolean = false;
  private callback: (() => void) | null = null;

  onDestroy(callback: () => void): () => void {
    this.callback = callback;
    return (): void => {
      this.callback = null;
    };
  }

  destroy(): void {
    this.destroyed = true;
    this.callback?.();
  }
}
