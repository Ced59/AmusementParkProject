import { Component, OnInit, Inject, PLATFORM_ID, Output, EventEmitter } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ApiService } from "../../../services/api.service";
import { ActivatedRoute, Router } from "@angular/router";
import { UserToken } from "../../../models/users/user_token";
import { AuthService } from "../../../services/auth/auth.service";
import { ToastMessageService } from "../../../services/messages/toast-message.service";
import {SharedService} from "../../../services/shared/shared.service";

@Component({
  selector: 'app-signin-google',
  templateUrl: './signin-google.component.html',
  styleUrls: ['./signin-google.component.scss']
})
export class SigninGoogleComponent implements OnInit {
  user: any = null;
  lastVisitedUrl: string | null = null;

  constructor(
    private apiService: ApiService,
    private route: ActivatedRoute,
    private router: Router,
    private messageService: ToastMessageService,
    private authService: AuthService,
    private sharedService: SharedService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.lastVisitedUrl = localStorage.getItem('lastVisitedUrl');
    }

    this.route.queryParams.subscribe(params => {
      const code = params['code'];
      console.log(code);
      if (code) {
        this.exchangeCodeForToken(code);
      }
    });
  }

  exchangeCodeForToken(code: string) {
    this.apiService.googleLogin(code).subscribe({
      next: (result: UserToken) => {
        this.authService.setToken(result.token);
        this.messageService.add('success', 'Succès', 'Connexion avec Google réussie !');
        this.sharedService.emitLoginStatusChange();

        this.redirectToLastVisitedUrl(this.lastVisitedUrl);
      },
      error: (error) => {
        if (error.status === 403 && error.error) {
          this.messageService.add('error', 'Erreur', error.error);
        } else {
          this.messageService.add('error', 'Erreur', "Une erreur inattendue est survenue.");
        }
        this.redirectToLastVisitedUrl(this.lastVisitedUrl);
      }
    });
  }

  redirectToLastVisitedUrl(url: string | null) {
    if (url) {
      this.router.navigateByUrl(url);
    } else {
      // Si aucun URL n'est trouvé, redirigez vers un fallback par défaut
      this.router.navigateByUrl('/en/home');
    }
  }
}
