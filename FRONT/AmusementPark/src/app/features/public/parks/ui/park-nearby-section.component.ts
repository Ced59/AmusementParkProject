import { Component, Input } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

import { ParkCardComponent } from '@app/components/public/park-card/park-card.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiDistancePanelComponent } from '@ui/maps';
import { UiSectionHeaderComponent } from '@ui/primitives';

@Component({
    selector: 'app-park-nearby-section',
    templateUrl: './park-nearby-section.component.html',
    styleUrls: ['./park-nearby-section.component.scss'],
    imports: [PageStateComponent, NgFor, ParkCardComponent, TranslateModule, UiDistancePanelComponent, UiSectionHeaderComponent]
})
export class ParkNearbySectionComponent {
  @Input() parks: ParkCardModel[] = [];
  @Input() currentLang: string = 'en';
  @Input() sourceName: string | null = null;
  @Input() state: ScreenStateKind = 'empty';

  protected get nearestPark(): ParkCardModel | null {
    return this.parks[0] ?? null;
  }
}
