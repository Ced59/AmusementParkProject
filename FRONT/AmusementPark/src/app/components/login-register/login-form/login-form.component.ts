import { Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';
import { UserCredentials } from '@app/models/users/user_credentials';
import { UserToken } from '@app/models/users/user_token';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SharedService } from '@app/services/shared/shared.service';
import { ModalService } from '@app/services/modal/modal.service';
import { FormsModule } from '@angular/forms';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

import { extractSafeDisplayErrorMessage } from '@shared/utils/security';
@Component({
    selector: 'app-login-form',
    templateUrl: './login-form.component.html',
    styleUrls: ['./login-form.component.scss'],
    imports: [FormsModule, Bind, InputText, ButtonDirective, TranslateModule]
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
    private readonly modalService: ModalService) {
  }

  onSubmit(): void {
    const userCredentials: UserCredentials = new UserCredentials(this.loginEmail, this.loginPassword);

    this.authApiService.login(userCredentials).subscribe({
      next: (result: UserToken) => {
        this.authService.setAuthenticatedSession(result);
        this.messageService.add('success', 'Succès', 'Connexion réussie !');
        this.sharedService.emitLoginStatusChange();
        this.loginSuccess.emit(result);
      },
      error: (error: unknown): void => {
        const errorMessage: string = extractSafeDisplayErrorMessage(error, 'Une erreur inattendue est survenue.');
        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
  }

  navigateToForgotPassword(): void {
    const currentLanguage: string = this.router.url.split('/')[1] || 'en';
    this.modalService.closeModal('loginModal');
    this.router.navigate(['/', currentLanguage, 'forgot-password']);
  }
}
