import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { NearbyDestinationKind } from '@app/models/parks/nearby-destination';
import { NearbyDestinationsFacade } from '@features/public/nearby-destinations/nearby-destinations.facade';
import { UiParkCardComponent } from '@ui/cards';
import { UiDistancePanelComponent } from '@ui/maps';
import { UiSectionHeaderComponent } from '@ui/primitives';

@Component({
  selector: 'app-park-nearby-section',
  providers: [NearbyDestinationsFacade],
  templateUrl: './park-nearby-section.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ['./park-nearby-section.component.scss'],
  imports: [PageStateComponent, NgFor, TranslateModule, UiParkCardComponent, UiDistancePanelComponent, UiSectionHeaderComponent]
})
export class ParkNearbySectionComponent implements OnChanges {
  private readonly facade = inject(NearbyDestinationsFacade);
  protected readonly destinations = this.facade.cards;
  protected readonly state = this.facade.state;
  @Input() sourceId: string | null = null;
  @Input() sourceKind: NearbyDestinationKind = 'Park';
  @Input() currentLang: string = 'en';
  @Input() sourceName: string | null = null;
  @Input() alternativesMode: boolean = false;
  ngOnChanges(changes: SimpleChanges): void {
    this.facade.setLanguage(this.currentLang);
    if (changes['sourceId'] || changes['sourceKind']) {
      this.facade.load(this.sourceKind, this.sourceId);
    }
  }

  protected get nearestPark(): ParkCardModel | null {
    return this.destinations()[0]?.card ?? null;
  }

  protected get titleKey(): string {
    return this.alternativesMode ? 'parks.detail.nearby.alternativesTitle' : 'parks.detail.nearby.title';
  }

  protected get subtitleKey(): string {
    return this.alternativesMode ? 'parks.detail.nearby.alternativesSubtitle' : 'parks.detail.nearby.subtitle';
  }
}
