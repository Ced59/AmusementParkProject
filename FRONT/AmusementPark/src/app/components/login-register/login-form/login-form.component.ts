import { Component, EventEmitter, Output } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { UserCredentials } from '@app/models/users/user_credentials';
import { UserToken } from '@app/models/users/user_token';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SharedService } from '@app/services/shared/shared.service';
import { ModalService } from '@app/services/modal/modal.service';
import { FormsModule } from '@angular/forms';
import { UiButtonDirective } from '@ui/primitives';
import { UiFieldInputComponent } from '@ui/forms';
import { UiKickerComponent } from '@ui/primitives';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.scss'],
  imports: [FormsModule, TranslateModule, UiButtonDirective, UiFieldInputComponent, UiKickerComponent]
})
export class LoginFormComponent {
  loginEmail: string = '';
  loginPassword: string = '';

  @Output() loginSuccess: EventEmitter<UserToken> = new EventEmitter<UserToken>();

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly messageService: ToastMessageService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly translateService: TranslateService) {
  }

  async onSubmit(): Promise<void> {
    const userCredentials: UserCredentials = new UserCredentials(this.loginEmail, this.loginPassword);

    try {
      const result: UserToken = await firstValueFrom(this.authApiService.login(userCredentials));
      this.authService.setAuthenticatedSession(result);
      this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('auth.login.success', 'Signed in successfully.'));
      this.sharedService.emitLoginStatusChange();
      this.loginSuccess.emit(result);
    } catch (error: unknown) {
      const errorMessage: string = extractSafeDisplayErrorMessage(error, this.translate('common.unexpectedError', 'An unexpected error occurred.'));
      this.messageService.add('error', this.translate('common.error', 'Error'), errorMessage);
    }
  }

  navigateToForgotPassword(): void {
    const currentLanguage: string = this.router.url.split('/')[1] || 'en';
    this.modalService.closeModal('loginModal');
    this.router.navigate(['/', currentLanguage, 'forgot-password']);
  }

  private translate(key: string, fallback: string): string {
    const translatedValue: string = this.translateService.instant(key);
    return translatedValue === key ? fallback : translatedValue;
  }
}
