import { Component, Input } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiParkCardComponent } from '@ui/cards';
import { UiDistancePanelComponent } from '@ui/maps';
import { UiSectionHeaderComponent } from '@ui/primitives';

@Component({
    selector: 'app-park-nearby-section',
    templateUrl: './park-nearby-section.component.html',
    styleUrls: ['./park-nearby-section.component.scss'],
    imports: [PageStateComponent, NgFor, TranslateModule, UiParkCardComponent, UiDistancePanelComponent, UiSectionHeaderComponent]
})
export class ParkNearbySectionComponent {
  @Input() parks: ParkCardModel[] = [];
  @Input() currentLang: string = 'en';
  @Input() sourceName: string | null = null;
  @Input() state: ScreenStateKind = 'empty';

  protected buildParkLink(park: ParkCardModel): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang,
      parkId: park.id,
      parkName: park.name
    });
  }

  protected get nearestPark(): ParkCardModel | null {
    return this.parks[0] ?? null;
  }
}
