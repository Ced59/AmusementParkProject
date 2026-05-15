import { Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiCardActionModel, UiInfoCardComponent, UiInfoCardMetricModel } from '@ui/cards';
import { UiSectionHeaderComponent } from '@ui/primitives';
import { ParkDetailViewModel } from '../models/park-detail-view.model';

@Component({
  selector: 'app-park-practical-info-section',
  templateUrl: './park-practical-info-section.component.html',
  styleUrls: ['./park-practical-info-section.component.scss'],
  imports: [TranslateModule, UiInfoCardComponent, UiSectionHeaderComponent]
})
export class ParkPracticalInfoSectionComponent {
  @Input() park: ParkDetailViewModel | null = null;

  get metrics(): UiInfoCardMetricModel[] {
    const currentPark: ParkDetailViewModel | null = this.park;

    if (!currentPark) {
      return [];
    }

    const metrics: UiInfoCardMetricModel[] = [];

    if (currentPark.countryCode) {
      metrics.push({ labelKey: 'parks.fields.country', value: currentPark.countryCode, iconClass: 'pi pi-flag' });
    }

    if (currentPark.city) {
      metrics.push({ labelKey: 'parks.fields.city', value: currentPark.city, iconClass: 'pi pi-building' });
    }

    if (currentPark.addressLine) {
      metrics.push({ labelKey: 'parks.fields.address', value: currentPark.addressLine, iconClass: 'pi pi-map-marker' });
    }

    return metrics;
  }

  get websiteAction(): UiCardActionModel | null {
    if (!this.park?.websiteUrl) {
      return null;
    }

    return {
      labelKey: 'parks.actions.website',
      iconClass: 'pi pi-external-link',
      externalUrl: this.park.websiteUrl
    };
  }
}
