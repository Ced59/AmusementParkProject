import { ChangeDetectionStrategy, Component, OnDestroy, Signal } from '@angular/core';

import { TranslateModule } from '@ngx-translate/core';

import {
  ADMIN_PUBLIC_VIEW_MODE_DEFINITIONS,
  AdminPublicViewMode,
  AdminPublicViewModeDefinition
} from '../../models/admin-public-view-mode.model';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';

@Component({
  selector: 'app-admin-public-view-toolbar',
  templateUrl: './admin-public-view-toolbar.component.html',
  styleUrl: './admin-public-view-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class AdminPublicViewToolbarComponent implements OnDestroy {
  protected readonly viewModeDefinitions: readonly AdminPublicViewModeDefinition[] = ADMIN_PUBLIC_VIEW_MODE_DEFINITIONS;
  protected readonly viewMode: Signal<AdminPublicViewMode> = this.adminPublicViewModeFacade.viewMode;
  protected readonly editionModeEnabled: Signal<boolean> = this.adminPublicViewModeFacade.editionModeEnabled;
  protected readonly canEdit: Signal<boolean> = this.adminPublicViewModeFacade.canEdit;

  constructor(private readonly adminPublicViewModeFacade: AdminPublicViewModeFacade) {
  }

  ngOnDestroy(): void {
    this.adminPublicViewModeFacade.reset();
  }

  protected selectViewMode(viewMode: AdminPublicViewMode): void {
    this.adminPublicViewModeFacade.setViewMode(viewMode);
  }

  protected toggleEditionMode(): void {
    this.adminPublicViewModeFacade.toggleEditionMode();
  }
}
