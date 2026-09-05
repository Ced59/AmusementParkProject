import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { HomeLatestArticleCardModel } from '@app/models/home/home-latest-article-card.model';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiButtonDirective } from '@ui/primitives';

@Component({
  selector: 'app-ui-article-card',
  templateUrl: './ui-article-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, ImageDisplayComponent, UiButtonDirective]
})
export class UiArticleCardComponent {
  @Input() article: HomeLatestArticleCardModel | null = null;
}
