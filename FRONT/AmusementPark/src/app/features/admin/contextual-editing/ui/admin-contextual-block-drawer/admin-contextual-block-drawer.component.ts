import { ChangeDetectionStrategy, Component, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminContextualBlockCapability,
  AdminContextualBlockInstance
} from '../../models/admin-contextual-block.model';
import { AdminContextualBlockExportFacade } from '../../state/admin-contextual-block-export.facade';
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
  protected readonly isExporting: Signal<boolean> = this.exportFacade.isExporting;
  protected readonly exportErrorKey: Signal<string | null> = this.exportFacade.errorKey;

  constructor(
    private readonly selectionFacade: AdminContextualBlockSelectionFacade,
    private readonly exportFacade: AdminContextualBlockExportFacade
  ) {
  }

  protected close(): void {
    this.selectionFacade.clearSelection();
  }

  protected canDownloadJson(block: AdminContextualBlockInstance): boolean {
    return this.exportFacade.canExport(block);
  }

  protected downloadJson(block: AdminContextualBlockInstance): void {
    this.exportFacade.exportBlock(block);
  }

  protected getIdEntries(block: AdminContextualBlockInstance): AdminContextualBlockIdEntry[] {
    return Object.entries(block.ids).map(([key, value]: [string, string]) => ({ key, value }));
  }

  protected getCapabilityLabelKey(capability: AdminContextualBlockCapability): string {
    return `admin.contextualBlocks.capabilities.${capability}`;
  }
}
