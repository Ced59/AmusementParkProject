import { AfterViewInit, Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { GoogleIdentityService } from '@app/services/auth/google-identity.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { SharedService } from '@app/services/shared/shared.service';
import { UserToken } from '@app/models/users/user_token';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RegisterFormComponent } from '../register-form/register-form.component';
import { LoginFormComponent } from '../login-form/login-form.component';

import { extractSafeDisplayErrorMessage } from '@shared/utils/security';
@Component({
    selector: 'app-auth-modal',
    templateUrl: './auth-modal.component.html',
    styleUrls: ['./auth-modal.component.scss'],
    imports: [Bind, ButtonDirective, RegisterFormComponent, LoginFormComponent]
})
export class AuthModalComponent implements AfterViewInit {
  @Output() closeModal: EventEmitter<void> = new EventEmitter<void>();
  @ViewChild('googleButtonContainer', { static: true })
  private googleButtonContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly googleIdentityService: GoogleIdentityService,
    private readonly messageService: ToastMessageService,
    private readonly sharedService: SharedService) {
  }

  async ngAfterViewInit(): Promise<void> {
    await this.renderGoogleButtonAsync();
  }

  onLoginSuccess(): void {
    this.closeModal.emit();
  }

  private async renderGoogleButtonAsync(): Promise<void> {
    if (!this.googleButtonContainer) {
      return;
    }

    try {
      await this.googleIdentityService.renderButtonAsync(
        this.googleButtonContainer.nativeElement,
        (response: GoogleCredentialResponse) => {
          this.authenticateWithGoogle(response.credential);
        });
    } catch (error) {
      console.error('Unable to render Google button.', error);
      this.messageService.add('error', 'Erreur', 'La connexion Google est temporairement indisponible.');
    }
  }

  private authenticateWithGoogle(idToken: string): void {
    this.authApiService.externalLogin('google', idToken).subscribe({
      next: (result: UserToken) => {
        this.authService.setAuthenticatedSession(result);
        this.messageService.add('success', 'Succès', 'Connexion avec Google réussie !');
        this.sharedService.emitLoginStatusChange();
        this.closeModal.emit();
      },
      error: (error: unknown): void => {
        const errorMessage: string = extractSafeDisplayErrorMessage(error, 'Une erreur inattendue est survenue.');
        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
  }
}
