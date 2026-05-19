import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { ForgotPasswordPageStateFacade } from '@features/auth/state/forgot-password-page-state.facade';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiFieldInputComponent } from '@ui/forms';

@Component({
  selector: 'app-forgot-password-page',
  templateUrl: './forgot-password-page.component.html',
  styleUrls: ['./forgot-password-page.component.scss'],
  providers: [ForgotPasswordPageStateFacade],
  imports: [FormsModule, TranslateModule, PageStateComponent, UiButtonDirective, UiChipComponent, UiFieldInputComponent, UiSectionHeaderComponent, UiSurfaceDirective]
})
export class ForgotPasswordPageComponent {
  protected readonly state = this.stateFacade.state;
  protected readonly email = this.stateFacade.email;
  protected readonly isSubmitted = this.stateFacade.isSubmitted;
  protected readonly message = this.stateFacade.message;

  constructor(private readonly stateFacade: ForgotPasswordPageStateFacade) {
  }

  onEmailChanged(email: string): void {
    this.stateFacade.setEmail(email);
  }

  onSubmit(): void {
    this.stateFacade.submit();
  }
}
