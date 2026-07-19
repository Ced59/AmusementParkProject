import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts';
import { LocalizedPluralPipe, SafeExternalUrlPipe } from '@shared/pipes';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { PublicVideoBackLink } from './public-video-list-view.component';
import { PublicVideoNavigationItem, PublicVideoWatchViewModel } from '../models/public-video-view.model';

@Component({
  selector: 'app-public-video-watch-view',
  templateUrl: './public-video-watch-view.component.html',
  styleUrls: ['./public-video-watch-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ImageDisplayComponent,
    PageStateComponent,
    RouterLink,
    SafeExternalUrlPipe,
    LocalizedPluralPipe,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective,
    PublicSharePanelComponent
  ]
})
export class PublicVideoWatchViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() video: PublicVideoWatchViewModel | null = null;
  @Input() previousVideo: PublicVideoNavigationItem | null = null;
  @Input() nextVideo: PublicVideoNavigationItem | null = null;
  @Input() backLinks: PublicVideoBackLink[] = [];
  @Input() loadingTitleKey: string = 'videos.watch.loadingTitle';
  @Input() loadingMessageKey: string = 'videos.watch.loadingMessage';
  @Input() errorTitleKey: string = 'videos.watch.errorTitle';
  @Input() errorMessageKey: string = 'videos.watch.errorMessage';

  protected readonly posterResponsiveWidths: readonly number[] = [640, 960, 1280];
}
