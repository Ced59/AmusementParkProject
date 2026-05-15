import { Component, Input } from '@angular/core';

import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiParkCardComponent } from '@ui/cards';

@Component({
  selector: 'app-park-card',
  templateUrl: './park-card.component.html',
  imports: [UiParkCardComponent]
})
export class ParkCardComponent {
  @Input() park: ParkCardModel | null = null;
  @Input() currentLang = 'en';
  @Input() compact = false;

  get parkLink(): string[] | null {
    if (!this.park?.id || !this.park.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name)];
  }
}
