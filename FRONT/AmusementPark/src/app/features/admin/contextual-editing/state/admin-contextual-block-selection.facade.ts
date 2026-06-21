import { Injectable, Signal, computed, effect, signal } from '@angular/core';

import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { AdminPublicViewModeFacade } from './admin-public-view-mode.facade';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockSelectionFacade {
  private readonly selectedBlockSignal = signal<AdminContextualBlockInstance | null>(null);

  public readonly selectedBlock: Signal<AdminContextualBlockInstance | null> = this.selectedBlockSignal.asReadonly();
  public readonly hasSelection: Signal<boolean> = computed(() => this.selectedBlockSignal() !== null);

  constructor(private readonly adminPublicViewModeFacade: AdminPublicViewModeFacade) {
    effect((): void => {
      if (!this.adminPublicViewModeFacade.editionModeEnabled()) {
        this.clearSelection();
      }
    });
  }

  selectBlock(block: AdminContextualBlockInstance | null): void {
    if (!block || !this.adminPublicViewModeFacade.editionModeEnabled()) {
      return;
    }

    this.selectedBlockSignal.set(block);
  }

  clearSelection(): void {
    this.selectedBlockSignal.set(null);
  }
}
