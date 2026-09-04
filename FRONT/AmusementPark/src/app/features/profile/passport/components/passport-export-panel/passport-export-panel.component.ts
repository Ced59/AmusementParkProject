import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PassportExportFormat } from '@app/models/passport/passport-export.models';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PassportExportStateFacade } from '../../state/passport-export-state.facade';

@Component({
  selector: 'app-passport-export-panel',
  templateUrl: './passport-export-panel.component.html',
  styleUrl: './passport-export-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportExportStateFacade],
  imports: [TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class PassportExportPanelComponent {
  protected readonly facade = inject(PassportExportStateFacade);

  protected request(format: PassportExportFormat): void {
    this.facade.request(format);
  }
}
