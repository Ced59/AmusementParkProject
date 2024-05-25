import { Component } from '@angular/core';
import {UserCredentials} from "../../../models/users/user_credentials";
import {ApiService} from "../../../services/api.service";

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrl: './login-form.component.scss'
})
export class LoginFormComponent {
  loginEmail: string;
  loginPassword: string;

  constructor(private apiService: ApiService) {
    this.loginEmail = "";
    this.loginPassword = "";
  }

  onSubmit() {
    console.log('Email:', this.loginEmail);
    console.log('Password:', this.loginPassword);


    let userCredentials = new UserCredentials(this.loginEmail, this.loginPassword);

    this.apiService.login(userCredentials).subscribe(result => {
      console.log(result);
    });


  }
}
