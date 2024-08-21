import { Component } from '@angular/core';
import {UserCredentials} from "../../../models/users/user_credentials";
import {ApiService} from "../../../services/api.service";
import {ToastMessageService} from "../../../services/messages/toast-message.service";
import {UserToken} from "../../../models/users/user_token";

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss'
})
export class LoginFormComponent {
  loginEmail: string;
  loginPassword: string;

  constructor(private apiService: ApiService, private messageService: ToastMessageService) {
    this.loginEmail = "";
    this.loginPassword = "";
  }

  onSubmit() {
    console.log('Email:', this.loginEmail);
    console.log('Password:', this.loginPassword);


    let userCredentials = new UserCredentials(this.loginEmail, this.loginPassword);

    this.apiService.login(userCredentials).subscribe({
      next: (result: UserToken) => {
        // Gérer la connexion réussie
        localStorage.setItem('auth_token', result.token)
        this.messageService.add('success', 'Succès', 'Connexion réussie !');
      },
      error: (error) => {
        if (error.status === 403 && error.error && error.error) {
          this.messageService.add('error', 'Erreur', error.error);
        } else {
          this.messageService.add('error', 'Erreur', "Une erreur inattendue est survenue.");
        }
      }
    });

  }
}
