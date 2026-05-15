import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { UiChipComponent } from '@ui/primitives';

@Component({
    selector: 'app-park-hero-section',
    templateUrl: './park-hero-section.component.html',
    styleUrls: ['./park-hero-section.component.scss'],
    imports: [NgIf, ImageDisplayComponent, TranslateModule, SafeExternalUrlPipe, SafeRichHtmlPipe, UiChipComponent]
})
export class ParkHeroSectionComponent {
  @Input() park: ParkDetailViewModel | null = null;

  get hasLogoImageId(): boolean {
    return !!this.park?.logoImageId?.trim();
  }
}
