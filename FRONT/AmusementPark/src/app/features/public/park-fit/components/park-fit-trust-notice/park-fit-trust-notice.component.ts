import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-fit-trust-notice',
  templateUrl: './park-fit-trust-notice.component.html',
  styleUrl: './park-fit-trust-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiSurfaceDirective]
})
export class ParkFitTrustNoticeComponent {
}
