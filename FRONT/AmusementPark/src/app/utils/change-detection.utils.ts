import { ChangeDetectorRef } from '@angular/core';

export function commitViewUpdate(changeDetectorRef: ChangeDetectorRef, update: () => void): void {
  queueMicrotask((): void => {
    update();
    changeDetectorRef.markForCheck();
  });
}
