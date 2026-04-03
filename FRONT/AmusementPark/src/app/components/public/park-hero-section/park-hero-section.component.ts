import { Component, Input } from '@angular/core';
import { Park } from '../../../models/parks/park';
import { resolveLocalizedValue } from '../../../commons/localized-item.utils';
import { buildParkAddressLine, buildParkLocationLine } from '../../../commons/park-presentation.utils';
import { NgIf } from '@angular/common';
import { ImageDisplayComponent } from '../../shared/image-display/image-display.component';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-park-hero-section',
    templateUrl: './park-hero-section.component.html',
    styleUrls: ['./park-hero-section.component.scss'],
    imports: [NgIf, ImageDisplayComponent, TranslateModule]
})
export class ParkHeroSectionComponent {
  @Input() park: Park | null = null;
  @Input() currentLang = 'en';

  get hasLogoImageId(): boolean {
    return !!this.park?.currentLogoImageId?.trim();
  }

  get locationLine(): string | null {
    return buildParkLocationLine(this.park);
  }

  get addressLine(): string | null {
    return buildParkAddressLine(this.park);
  }

  get description(): string | null {
    return resolveLocalizedValue(this.park?.descriptions, this.currentLang) ?? null;
  }
}
