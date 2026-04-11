import { Component, Inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { UserToken } from '../../../models/users/user_token';
import { AuthService } from '../../../services/auth/auth.service';
import { ToastMessageService } from '../../../services/messages/toast-message.service';
import { SharedService } from '../../../services/shared/shared.service';
import { CurrentUserService } from '../../../services/users/current-user.service';
import { ViewState } from '../../../models/shared/view-state';

@Component({
  selector: 'app-signin-google',
  templateUrl: './signin-google.component.html',
  styleUrls: ['./signin-google.component.scss']
})
export class SigninGoogleComponent implements OnInit {
  readonly viewState = signal<ViewState>(ViewState.Loading);

  lastVisitedUrl: string | null = null;

  constructor(
    private readonly authApiService: AuthApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly messageService: ToastMessageService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly currentUserService: CurrentUserService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.lastVisitedUrl = localStorage.getItem('lastVisitedUrl');
    }

    this.route.queryParams.subscribe((params) => {
      const code = params['code'];

      if (!code) {
        this.viewState.set(ViewState.Error);
        return;
      }

      this.exchangeCodeForToken(code);
    });
  }

  protected readonly states = ViewState;

  private exchangeCodeForToken(code: string): void {
    this.viewState.set(ViewState.Loading);

    this.authApiService.googleLogin(code).subscribe({
      next: (result: UserToken) => {
        this.authService.setAuthenticatedSession(result);
        this.currentUserService.refreshCurrentUser();
        this.messageService.add('success', 'Succès', 'Connexion avec Google réussie !');
        this.sharedService.emitLoginStatusChange();
        this.redirectToLastVisitedUrl(this.lastVisitedUrl);
      },
      error: (error: any) => {
        this.viewState.set(ViewState.Error);

        if (error.status === 403 && error.error) {
          this.messageService.add('error', 'Erreur', error.error);
        } else {
          this.messageService.add('error', 'Erreur', 'Une erreur inattendue est survenue.');
        }

        this.redirectToLastVisitedUrl(this.lastVisitedUrl);
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
