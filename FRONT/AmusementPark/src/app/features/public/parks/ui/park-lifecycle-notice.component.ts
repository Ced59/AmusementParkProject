import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { ParkStatus } from '@app/models/parks/park-status';
import { ParkLifecycleNoticeModel, resolveParkLifecycleNotice } from '../models/park-lifecycle-notice.model';

@Component({
  selector: 'app-park-lifecycle-notice',
  templateUrl: './park-lifecycle-notice.component.html',
  styleUrls: ['./park-lifecycle-notice.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class ParkLifecycleNoticeComponent {
  @Input({ required: true }) status: ParkStatus | null | undefined;
  @Input() parkName: string | null | undefined = null;

  protected get notice(): ParkLifecycleNoticeModel | null {
    return resolveParkLifecycleNotice(this.status);
  }
}
