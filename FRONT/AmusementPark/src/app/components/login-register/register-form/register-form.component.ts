import { Component } from '@angular/core';
import { UserRegister } from '@app/models/users/user-register';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslationService } from '@app/services/translation.service';
import { FormsModule } from '@angular/forms';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { NgClass } from '@angular/common';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

import { extractSafeDisplayErrorMessage } from '@shared/utils/security';
@Component({
    selector: 'app-register-form',
    templateUrl: './register-form.component.html',
    styleUrls: ['./register-form.component.scss'],
    imports: [FormsModule, Bind, InputText, NgClass, ButtonDirective, TranslateModule]
})
export class RegisterFormComponent {
  registerEmail: string = '';
  registerPassword: string = '';
  confirmPassword: string = '';
  registrationCompleted: boolean = false;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly messageService: ToastMessageService,
    private readonly translateService: TranslationService) {
  }

  onSubmit(): void {
    if (!this.isValidPassword() || !this.isValidEmail() || !this.passwordsMatch()) {
      this.messageService.add('error', 'Erreur', 'Le formulaire d’inscription contient des erreurs.');
      return;
    }

    const request: UserRegister = {
      email: this.registerEmail,
      password: this.registerPassword,
      verifyPassword: this.confirmPassword,
      preferredLanguage: this.translateService.getCurrentLang().toUpperCase()
    };

    this.authApiService.register(request).subscribe({
      next: () => {
        this.registrationCompleted = true;
        this.messageService.add('success', 'Succès', 'Compte créé. Vérifie le lien de confirmation envoyé dans la console de l’API.');
      },
      error: (error: unknown): void => {
        const errorMessage: string = extractSafeDisplayErrorMessage(error, 'Une erreur inattendue est survenue.');
        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
  }

  resendConfirmation(): void {
    this.authApiService.resendConfirmation(this.registerEmail).subscribe({
      next: (response) => {
        this.messageService.add('success', 'Succès', response.message);
      },
      error: (error: unknown): void => {
        const errorMessage: string = extractSafeDisplayErrorMessage(error, 'Une erreur inattendue est survenue.');
        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
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
}
