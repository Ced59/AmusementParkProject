import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe } from '@shared/pipes';

import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ImageDisplayComponent } from '../../shared/image-display/image-display.component';
import { UiButtonDirective, UiChipComponent } from '@ui/primitives';

@Component({
    selector: 'app-park-card',
    templateUrl: './park-card.component.html',
    styleUrls: ['./park-card.component.scss'],
    imports: [NgIf, ImageDisplayComponent, RouterLink, TranslateModule, SafeExternalUrlPipe, UiButtonDirective, UiChipComponent]
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

  get hasLogoImageId(): boolean {
    return !!this.park?.logoImageId?.trim();
  }
}
