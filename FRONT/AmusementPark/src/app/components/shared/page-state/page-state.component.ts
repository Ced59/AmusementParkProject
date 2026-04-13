import { Component, Input } from '@angular/core';
import { ViewState } from '@app/models/shared/view-state';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { PageStateMessages, ScreenStateKind } from '@shared/models/contracts';

@Component({
    selector: 'app-page-state',
    templateUrl: './page-state.component.html',
    styleUrls: ['./page-state.component.scss'],
    imports: [NgSwitch, NgSwitchCase, NgSwitchDefault, EmptyStateComponent]
})
export class PageStateComponent {
  @Input() state: ViewState | ScreenStateKind = ViewState.Ready;
  @Input() messages: PageStateMessages | null = null;

  @Input() loadingTitleKey: string = 'common.loadingTitle';
  @Input() loadingMessageKey: string = 'common.loadingMessage';
  @Input() errorTitleKey: string = 'common.errorTitle';
  @Input() errorMessageKey: string = 'common.errorMessage';
  @Input() emptyTitleKey: string = 'common.emptyTitle';
  @Input() emptyMessageKey: string = 'common.emptyMessage';

  protected readonly viewState = ViewState;

  protected get resolvedLoadingTitleKey(): string {
    return this.messages?.loadingTitleKey ?? this.loadingTitleKey;
  }

  protected get resolvedLoadingMessageKey(): string {
    return this.messages?.loadingMessageKey ?? this.loadingMessageKey;
  }

  protected get resolvedErrorTitleKey(): string {
    return this.messages?.errorTitleKey ?? this.errorTitleKey;
  }

  protected get resolvedErrorMessageKey(): string {
    return this.messages?.errorMessageKey ?? this.errorMessageKey;
  }

  protected get resolvedEmptyTitleKey(): string {
    return this.messages?.emptyTitleKey ?? this.emptyTitleKey;
  }

  protected get resolvedEmptyMessageKey(): string {
    return this.messages?.emptyMessageKey ?? this.emptyMessageKey;
  }
}
