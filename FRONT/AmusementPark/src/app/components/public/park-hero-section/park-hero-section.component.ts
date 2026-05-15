import { Component, Input } from '@angular/core';
import { Park } from '@app/models/parks/park';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { buildParkAddressLine, buildParkLocationLine } from '@app/commons/park-presentation.utils';
import { NgIf } from '@angular/common';
import { ImageDisplayComponent } from '../../shared/image-display/image-display.component';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';

@Component({
    selector: 'app-park-hero-section',
    templateUrl: './park-hero-section.component.html',
    styleUrls: ['./park-hero-section.component.scss'],
    imports: [NgIf, ImageDisplayComponent, TranslateModule, SafeExternalUrlPipe, SafeRichHtmlPipe]
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
