import { Component } from '@angular/core';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss'
})
export class LoginFormComponent {
  loginEmail: string; // Définissez la propriété pour l'email
  loginPassword: string; // Définissez la propriété pour le mot de passe

  constructor() {
    this.loginEmail = '';
    this.loginPassword = '';
  }
}
