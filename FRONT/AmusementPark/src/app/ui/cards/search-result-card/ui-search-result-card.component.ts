import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiButtonDirective } from '@ui/primitives';
import { UiSearchResultCardModel } from '../models/ui-search-result-card.model';

@Component({
  selector: 'app-ui-search-result-card',
  templateUrl: './ui-search-result-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImageDisplayComponent, RouterLink, TranslateModule, SafeRichHtmlPipe, UiButtonDirective]
})
export class UiSearchResultCardComponent {
  @Input() card: UiSearchResultCardModel | null = null;

  protected get toneClass(): string {
    return `ui-search-result-card--${this.card?.tone ?? 'primary'}`;
  }

  protected get hasLogoImage(): boolean {
    return !!this.card?.logoImageId?.trim();
  }
}
