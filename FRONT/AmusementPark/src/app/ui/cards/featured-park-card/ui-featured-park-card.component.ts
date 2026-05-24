import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { UiButtonDirective, UiChipComponent } from '@ui/primitives';

@Component({
  selector: 'app-ui-featured-park-card',
  templateUrl: './ui-featured-park-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, ImageDisplayComponent, UiButtonDirective, UiChipComponent]
})
export class UiFeaturedParkCardComponent {
  @Input() park: HomeFeaturedParkCardModel | null = null;

  protected get hasLogo(): boolean {
    return !!this.park?.logoImageId?.trim();
  }

  protected get hasMetrics(): boolean {
    return (this.park?.metrics.length ?? 0) > 0;
  }
}
