import { Injectable, Signal, computed, signal } from '@angular/core';

import {
  AdminPublicViewMode,
  DEFAULT_ADMIN_PUBLIC_VIEW_MODE,
  isAdminPublicViewMode
} from '../models/admin-public-view-mode.model';

@Injectable({
  providedIn: 'root'
})
export class AdminPublicViewModeFacade {
  private readonly viewModeSignal = signal<AdminPublicViewMode>(DEFAULT_ADMIN_PUBLIC_VIEW_MODE);
  private readonly editionModeEnabledSignal = signal<boolean>(false);

  public readonly viewMode: Signal<AdminPublicViewMode> = this.viewModeSignal.asReadonly();
  public readonly editionModeEnabled: Signal<boolean> = this.editionModeEnabledSignal.asReadonly();
  public readonly canEdit: Signal<boolean> = computed(() => this.viewModeSignal() === 'adminPreview');

  setViewMode(viewMode: AdminPublicViewMode): void {
    const nextViewMode: AdminPublicViewMode = isAdminPublicViewMode(viewMode)
      ? viewMode
      : DEFAULT_ADMIN_PUBLIC_VIEW_MODE;

    this.viewModeSignal.set(nextViewMode);

    if (nextViewMode !== 'adminPreview') {
      this.editionModeEnabledSignal.set(false);
    }
  }

  setEditionModeEnabled(isEnabled: boolean): void {
    if (!this.canEdit()) {
      this.editionModeEnabledSignal.set(false);
      return;
    }

    this.editionModeEnabledSignal.set(isEnabled);
  }

  toggleEditionMode(): void {
    this.setEditionModeEnabled(!this.editionModeEnabledSignal());
  }

  reset(): void {
    this.viewModeSignal.set(DEFAULT_ADMIN_PUBLIC_VIEW_MODE);
    this.editionModeEnabledSignal.set(false);
  }
}
