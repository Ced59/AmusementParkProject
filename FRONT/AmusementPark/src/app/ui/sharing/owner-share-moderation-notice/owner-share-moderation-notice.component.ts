import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-owner-share-moderation-notice',
  templateUrl: './owner-share-moderation-notice.component.html',
  styleUrl: './owner-share-moderation-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class OwnerShareModerationNoticeComponent {}
