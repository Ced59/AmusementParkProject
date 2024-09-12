import {Component, Output, EventEmitter} from '@angular/core';
import {ApiService} from "../../../services/api.service";
import {environment} from "../../../../environments/environment";

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'] // Correction de styleUrl -> styleUrls
})
export class AuthModalComponent {
  @Output() closeModal = new EventEmitter<void>();

  constructor() {
  }

  signInWithGoogle() {
    const clientId = environment.googleClientId;
    const redirectUri = encodeURIComponent(environment.redirectOAuthUri);
    const responseType = 'code';
    const state = encodeURIComponent(this.generateRandomString());
    localStorage.setItem('oauth_state', state);
    const scope = encodeURIComponent('openid profile email');
    const prompt = 'select_account';
    const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${redirectUri}&response_type=${responseType}&scope=${scope}&state=${state}&prompt=${prompt}`;

    window.open(authUrl, 'googleAuth', 'width=500,height=600,left=200,top=200');
  }

  generateRandomString() {
    const randomPool = new Uint8Array(32);
    crypto.getRandomValues(randomPool);
    let hex = '';
    for (let i = 0; i < randomPool.length; ++i) {
      hex += randomPool[i].toString(16);
    }
    return hex;
  }

  onLoginSuccess() {
    this.closeModal.emit();
  }
}
