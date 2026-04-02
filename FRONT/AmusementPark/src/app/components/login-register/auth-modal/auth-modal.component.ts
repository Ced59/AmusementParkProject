import { AfterViewInit, Component, ElementRef, EventEmitter, Output, ViewChild } from '@angular/core';
import { ApiService } from '../../../services/api.service';
import { AuthService } from '../../../services/auth/auth.service';
import { GoogleIdentityService } from '../../../services/auth/google-identity.service';
import { ToastMessageService } from '../../../services/messages/toast-message.service';
import { SharedService } from '../../../services/shared/shared.service';
import { UserToken } from '../../../models/users/user_token';

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'],
  standalone: false
})
export class AuthModalComponent implements AfterViewInit {
  @Output() closeModal: EventEmitter<void> = new EventEmitter<void>();
  @ViewChild('googleButtonContainer', { static: true })
  private googleButtonContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly apiService: ApiService,
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
    this.apiService.externalLogin('google', idToken).subscribe({
      next: (result: UserToken) => {
        this.authService.setToken(result.token);
        this.messageService.add('success', 'Succès', 'Connexion avec Google réussie !');
        this.sharedService.emitLoginStatusChange();
        this.closeModal.emit();
      },
      error: (error: { error?: { Message?: string; message?: string; }; }): void => {
        const errorMessage: string = error.error?.Message
          ?? error.error?.message
          ?? 'Une erreur inattendue est survenue.';

        this.messageService.add('error', 'Erreur', errorMessage);
      }
    });
  }
}
