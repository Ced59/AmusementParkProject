import { Component } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { UserRegister } from '@app/models/users/user-register';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslationService } from '@app/services/translation.service';
import { FormsModule } from '@angular/forms';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { UiFieldInputComponent } from '@ui/forms';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security';

@Component({
  selector: 'app-register-form',
  templateUrl: './register-form.component.html',
  styleUrls: ['./register-form.component.scss'],
  imports: [FormsModule, TranslateModule, UiButtonDirective, UiChipComponent, UiFieldInputComponent, UiKickerComponent]
})
export class RegisterFormComponent {
  registerEmail: string = '';
  registerPassword: string = '';
  confirmPassword: string = '';
  registrationCompleted: boolean = false;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly messageService: ToastMessageService,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService) {
  }

  async onSubmit(): Promise<void> {
    if (!this.isValidPassword() || !this.isValidEmail() || !this.passwordsMatch()) {
      this.messageService.add('error', this.translate('common.error', 'Error'), this.translate('auth.register.validation_error', 'The registration form contains errors.'));
      return;
    }

    const request: UserRegister = {
      email: this.registerEmail,
      password: this.registerPassword,
      verifyPassword: this.confirmPassword,
      preferredLanguage: this.translationService.getCurrentLang().toUpperCase()
    };

    try {
      await firstValueFrom(this.authApiService.register(request));
      this.registrationCompleted = true;
      this.messageService.add('success', this.translate('common.success', 'Success'), this.translate('auth.register.created_toast', 'Account created. Check the confirmation link sent by the API.'));
    } catch (error: unknown) {
      const errorMessage: string = extractSafeDisplayErrorMessage(error, this.translate('common.unexpectedError', 'An unexpected error occurred.'));
      this.messageService.add('error', this.translate('common.error', 'Error'), errorMessage);
    }
  }

  async resendConfirmation(): Promise<void> {
    try {
      const response: { message: string } = await firstValueFrom(this.authApiService.resendConfirmation(this.registerEmail));
      this.messageService.add('success', this.translate('common.success', 'Success'), response.message);
    } catch (error: unknown) {
      const errorMessage: string = extractSafeDisplayErrorMessage(error, this.translate('common.unexpectedError', 'An unexpected error occurred.'));
      this.messageService.add('error', this.translate('common.error', 'Error'), errorMessage);
    }
  }

  isValidPassword(): boolean {
    const regex: RegExp = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$/;
    return regex.test(this.registerPassword);
  }

  isValidEmail(): boolean {
    const emailRegex: RegExp = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(this.registerEmail);
  }

  passwordsMatch(): boolean {
    return this.registerPassword === this.confirmPassword;
  }

  private translate(key: string, fallback: string): string {
    const translatedValue: string = this.translateService.instant(key);
    return translatedValue === key ? fallback : translatedValue;
  }
}
