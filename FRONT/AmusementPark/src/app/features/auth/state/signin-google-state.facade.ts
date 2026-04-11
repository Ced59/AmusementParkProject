import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UserToken } from '../../../../models/users/user_token';
import { AuthService } from '../../../../services/auth/auth.service';
import { ToastMessageService } from '../../../../services/messages/toast-message.service';
import { SharedService } from '../../../../services/shared/shared.service';
import { CurrentUserService } from '../../../../services/users/current-user.service';

@Injectable()
export class SigninGoogleStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<void>();

  public readonly state = this.screenStateStore.state;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly router: Router,
    private readonly messageService: ToastMessageService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly currentUserService: CurrentUserService
  ) {
  }

  handleCallback(code: string | null, lastVisitedUrl: string | null): void {
    if (!code) {
      this.screenStateStore.setError('auth.google.errorMessage');
      return;
    }

    this.screenStateStore.setLoading();

    this.authApiService.googleLogin(code).subscribe({
      next: (result: UserToken) => {
        this.authService.setAuthenticatedSession(result);
        this.currentUserService.refreshCurrentUser();
        this.messageService.add('success', 'Succès', 'Connexion avec Google réussie !');
        this.sharedService.emitLoginStatusChange();
        this.redirectToLastVisitedUrl(lastVisitedUrl);
      },
      error: (error: unknown) => {
        this.screenStateStore.setError('auth.google.errorMessage');

        if (typeof error === 'object' && error !== null && 'status' in error && 'error' in error) {
          const status: number = Number((error as { status?: unknown }).status);
          const nestedError: unknown = (error as { error?: unknown }).error;

          if (status === 403 && typeof nestedError === 'string') {
            this.messageService.add('error', 'Erreur', nestedError);
          } else {
            this.messageService.add('error', 'Erreur', 'Une erreur inattendue est survenue.');
          }
        } else {
          this.messageService.add('error', 'Erreur', 'Une erreur inattendue est survenue.');
        }

        this.redirectToLastVisitedUrl(lastVisitedUrl);
      }
    });
  }

  private redirectToLastVisitedUrl(url: string | null): void {
    if (url) {
      this.router.navigateByUrl(url);
      return;
    }

    this.router.navigateByUrl('/en/home');
  }
}
