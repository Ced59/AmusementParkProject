import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { UiButtonDirective, UiChipComponent } from '@ui/primitives';

@Component({
  selector: 'app-ui-park-card',
  templateUrl: './ui-park-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImageDisplayComponent, RouterLink, TranslateModule, SafeExternalUrlPipe, UiButtonDirective, UiChipComponent]
})
export class UiParkCardComponent {
  @Input() park: ParkCardModel | null = null;
  @Input() detailLink: string[] | null = null;
  @Input() compact: boolean = false;
  @Input() showWebsiteAction: boolean = true;
  @Input() primaryActionLabelKey: string = 'parks.actions.viewDetails';

  protected get hasLogoImageId(): boolean {
    return !!this.park?.logoImageId?.trim();
  }

  protected get hasSecondaryLocation(): boolean {
    return !!this.park?.city && !this.compact;
  }

  protected get hasDescription(): boolean {
    return !!this.park?.shortDescription && !this.compact;
  }

  protected get hasCoordinates(): boolean {
    return !!this.park?.coordinatesLine && !this.compact;
  }

  protected get canOpenWebsite(): boolean {
    return this.showWebsiteAction && !!this.park?.websiteUrl;
  }
}
