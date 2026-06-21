import { ChangeDetectionStrategy, Component, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminContextualBlockCapability,
  AdminContextualBlockInstance
} from '../../models/admin-contextual-block.model';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';

interface AdminContextualBlockIdEntry {
  readonly key: string;
  readonly value: string;
}

@Component({
  selector: 'app-admin-contextual-block-drawer',
  templateUrl: './admin-contextual-block-drawer.component.html',
  styleUrl: './admin-contextual-block-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class AdminContextualBlockDrawerComponent {
  protected readonly selectedBlock: Signal<AdminContextualBlockInstance | null> = this.selectionFacade.selectedBlock;

  constructor(private readonly selectionFacade: AdminContextualBlockSelectionFacade) {
  }

  protected close(): void {
    this.selectionFacade.clearSelection();
  }

  protected getIdEntries(block: AdminContextualBlockInstance): AdminContextualBlockIdEntry[] {
    return Object.entries(block.ids).map(([key, value]: [string, string]) => ({ key, value }));
  }

  protected getCapabilityLabelKey(capability: AdminContextualBlockCapability): string {
    return `admin.contextualBlocks.capabilities.${capability}`;
  }
}
