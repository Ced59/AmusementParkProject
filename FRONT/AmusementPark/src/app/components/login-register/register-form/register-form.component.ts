import { Component } from '@angular/core';

@Component({
  selector: 'app-register-form',
  templateUrl: './register-form.component.html',
  styleUrl: './register-form.component.scss'
})
export class RegisterFormComponent {
  registerEmail: string;
  registerPassword: string;
  confirmPassword: string;

  constructor() {
    this.registerEmail = '';
    this.registerPassword = '';
    this.confirmPassword = '';
  }

  // Méthode pour gérer la soumission du formulaire d'inscription
  register() {
    // Ajoutez ici la logique de traitement de l'inscription
    console.log('Inscription demandée avec : ', this.registerEmail, this.registerPassword);
  }
}
