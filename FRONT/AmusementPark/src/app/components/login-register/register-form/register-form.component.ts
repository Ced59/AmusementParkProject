import { Component } from '@angular/core';
import { MessageService } from 'primeng/api';
import {TranslationService} from "../../../services/translation.service";

@Component({
  selector: 'app-register-form',
  templateUrl: './register-form.component.html',
  styleUrls: ['./register-form.component.scss']
})
export class RegisterFormComponent {
  registerEmail: string;
  registerPassword: string;
  confirmPassword: string;

  constructor(
    private messageService: MessageService,
    private translateService: TranslationService
  ) {
    this.registerEmail = '';
    this.registerPassword = '';
    this.confirmPassword = '';
  }

  onSubmit() {
    if (!this.isValidPassword() || !this.isValidEmail() || !this.passwordsMatch()) {
      this.translateService.getTranslations([
        'auth.register.errors.password_error_summary',
        'auth.register.errors.password_error',
        'auth.register.errors.email_error'
      ]).subscribe(translations => {
        if (!this.isValidEmail()) {
          this.messageService.add({
            severity: 'error',
            summary: translations['auth.register.errors.email_error_summary'],
            detail: translations['auth.register.errors.email_error']
          });
        }
        if (!this.isValidPassword() || !this.passwordsMatch()) {
          this.messageService.add({
            severity: 'error',
            summary: translations['auth.register.errors.password_error_summary'],
            detail: translations['auth.register.errors.password_error']
          });
        }
      });
      return;
    }
    // Process registration here
  }



  isValidPassword(): boolean {
    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$/;
    return regex.test(this.registerPassword);
  }

  isValidEmail(): boolean {
    const emailRegex = /^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$/;
    return emailRegex.test(this.registerEmail);
  }

  passwordsMatch(): boolean {
    return this.registerPassword === this.confirmPassword;
  }



}
