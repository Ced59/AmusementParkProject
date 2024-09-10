// auth.guard.ts
import {Inject, Injectable, PLATFORM_ID} from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { ModalService } from "../services/modal/modal.service";
import { CanActivateFn } from '@angular/router';
import {isPlatformBrowser} from "@angular/common";

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {

  constructor(
    private authService: AuthService,
    private modalService: ModalService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  canActivate: CanActivateFn = () => {
    if (!this.authService.isLoggedIn()) {
      if (isPlatformBrowser(this.platformId)) {
        // Si nous sommes côté client, ouvrez la modal
        this.modalService.openLoginModal();
      } else {
        // Si nous sommes côté serveur, redirigez vers la page d'accueil
        this.router.navigate(['/']); // Assurez-vous que cette route est correctement configurée
      }
      return false;
    }
    return true;
  }
}
