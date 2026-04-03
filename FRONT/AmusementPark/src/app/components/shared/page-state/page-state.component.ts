import { Component, Input } from '@angular/core';
import { ViewState } from '../../../models/shared/view-state';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-page-state',
    templateUrl: './page-state.component.html',
    styleUrls: ['./page-state.component.scss'],
    imports: [NgSwitch, NgSwitchCase, NgSwitchDefault, TranslateModule]
})
export class PageStateComponent {
  @Input() state: ViewState = ViewState.Ready;

  @Input() loadingTitleKey: string = 'common.loadingTitle';
  @Input() loadingMessageKey: string = 'common.loadingMessage';

  @Input() errorTitleKey: string = 'common.errorTitle';
  @Input() errorMessageKey: string = 'common.errorMessage';

  @Input() emptyTitleKey: string = 'common.emptyTitle';
  @Input() emptyMessageKey: string = 'common.emptyMessage';

  protected readonly viewState = ViewState;
}
