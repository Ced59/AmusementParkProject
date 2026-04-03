import { Component } from '@angular/core';
import { UserRegister } from '../../../models/users/user-register';
import { ApiService } from '../../../services/api.service';
import { ToastMessageService } from '../../../services/messages/toast-message.service';
import { TranslationService } from '../../../services/translation.service';

@Component({
  selector: 'app-register-form',
  templateUrl: './register-form.component.html',
  styleUrls: ['./register-form.component.scss'],
  standalone: false
})
export class RegisterFormComponent {
  registerEmail: string = '';
  registerPassword: string = '';
  confirmPassword: string = '';
  registrationCompleted: boolean = false;

  constructor(
    private readonly apiService: ApiService,
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

    this.apiService.register(request).subscribe({
      next: () => {
        this.registrationCompleted = true;
        this.messageService.add('success', 'Succès', 'Compte créé. Vérifie le lien de confirmation envoyé dans la console de l’API.');
      },
      error: (error: { error?: { message?: string; Message?: string; }; }): void => {
        const errorMessage: string = error.error?.message
          ?? error.error?.Message
          ?? 'Une erreur inattendue est survenue.';

        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
  }

  resendConfirmation(): void {
    this.apiService.resendConfirmation(this.registerEmail).subscribe({
      next: (response) => {
        this.messageService.add('success', 'Succès', response.message);
      },
      error: (error: { error?: { message?: string; Message?: string; }; }): void => {
        const errorMessage: string = error.error?.message
          ?? error.error?.Message
          ?? 'Une erreur inattendue est survenue.';

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
