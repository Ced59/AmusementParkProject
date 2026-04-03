import {Component, Output, EventEmitter} from '@angular/core';
import {UserCredentials} from "../../../models/users/user_credentials";
import {ApiService} from "../../../services/api.service";
import {ToastMessageService} from "../../../services/messages/toast-message.service";
import {UserToken} from "../../../models/users/user_token";
import {AuthService} from "../../../services/auth/auth.service";
import {CurrentUserService} from '../../../services/users/current-user.service';
import {SharedService} from '../../../services/shared/shared.service';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.scss']
})
export class LoginFormComponent {
  loginEmail: string;
  loginPassword: string;

  @Output() loginSuccess = new EventEmitter<UserToken>();

  constructor(private apiService: ApiService,
              private messageService: ToastMessageService,
              private authService: AuthService,
              private currentUserService: CurrentUserService,
              private sharedService: SharedService) {
    this.loginEmail = "";
    this.loginPassword = "";
  }

  onSubmit() {
    let userCredentials = new UserCredentials(this.loginEmail, this.loginPassword);

    this.apiService.login(userCredentials).subscribe({
      next: (result: UserToken) => {
        this.authService.setToken(result.token);
        this.currentUserService.refreshCurrentUser();
        this.sharedService.emitLoginStatusChange();
        this.messageService.add('success', 'Succès', 'Connexion réussie !');
        this.loginSuccess.emit(result);
      },
      error: (error) => {
        if (error.status === 403 && error.error) {
          this.messageService.add('error', 'Erreur', error.error);
        } else {
          this.messageService.add('error', 'Erreur', "Une erreur inattendue est survenue.");
        }
      }
    });
  }
}
