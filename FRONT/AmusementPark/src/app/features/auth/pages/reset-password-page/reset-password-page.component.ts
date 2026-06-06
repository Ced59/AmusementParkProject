import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ResetPasswordPageStateFacade } from '@features/auth/state/reset-password-page-state.facade';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiFieldInputComponent } from '@ui/forms';

@Component({
  selector: 'app-reset-password-page',
  templateUrl: './reset-password-page.component.html',
  styleUrls: ['./reset-password-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ResetPasswordPageStateFacade],
  imports: [FormsModule, TranslateModule, PageStateComponent, UiButtonDirective, UiChipComponent, UiFieldInputComponent, UiSectionHeaderComponent, UiSurfaceDirective]
})
export class ResetPasswordPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly token = this.stateFacade.token;
  protected readonly newPassword = this.stateFacade.newPassword;
  protected readonly confirmPassword = this.stateFacade.confirmPassword;
  protected readonly isSubmitted = this.stateFacade.isSubmitted;
  protected readonly message = this.stateFacade.message;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: ResetPasswordPageStateFacade) {
  }

  ngOnInit(): void {
    const token: string = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.stateFacade.initialize(token);
  }

  onNewPasswordChanged(newPassword: string): void {
    this.stateFacade.setNewPassword(newPassword);
  }

  onConfirmPasswordChanged(confirmPassword: string): void {
    this.stateFacade.setConfirmPassword(confirmPassword);
  }

  onSubmit(): void {
    this.stateFacade.submit();
  }
}
