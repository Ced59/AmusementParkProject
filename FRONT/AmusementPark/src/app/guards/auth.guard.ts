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
    @Inject(PLATFORM_ID) private platformId: Object,
  ) {}

  canActivate: CanActivateFn = () => {
    if (!this.authService.isLoggedIn()) {
      if (isPlatformBrowser(this.platformId)) {
        this.modalService.openModal('loginModal')
      } else {
        const currentLang = this.router.url.split('/')[1] || 'en';
        this.router.navigate([currentLang, 'home']);
      }
      return false;
    }
    return true;
  }
}
