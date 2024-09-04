// auth.guard.ts
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { ModalService } from "../services/modal/modal.service";
import { CanActivateFn } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {

  constructor(private authService: AuthService, private modalService: ModalService, private router: Router) {}

  canActivate: CanActivateFn = () => {
    if (!this.authService.isLoggedIn()) {
      this.modalService.openLoginModal();  // Ouverture de la modal de connexion
      return false;
    }
    return true;
  }
}
