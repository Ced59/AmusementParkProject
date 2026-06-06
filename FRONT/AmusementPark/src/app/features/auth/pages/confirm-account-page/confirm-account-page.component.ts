import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { ConfirmAccountPageStateFacade } from '@features/auth/state/confirm-account-page-state.facade';

@Component({
    selector: 'app-confirm-account-page',
    templateUrl: './confirm-account-page.component.html',
    styleUrls: ['./confirm-account-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ConfirmAccountPageStateFacade],
    imports: [RouterLink, TranslateModule, PageStateComponent, UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective]
})
export class ConfirmAccountPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly currentLanguage = this.stateFacade.currentLanguage;
  protected readonly isSuccess = this.stateFacade.isSuccess;
  protected readonly message = this.stateFacade.message;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: ConfirmAccountPageStateFacade
  ) {
  }

  ngOnInit(): void {
    const currentLanguage: string = this.route.parent?.snapshot.paramMap.get('lang') ?? 'en';
    const token: string = this.route.snapshot.queryParamMap.get('token') ?? '';

    this.stateFacade.confirmEmail(token, currentLanguage);
  }
}
