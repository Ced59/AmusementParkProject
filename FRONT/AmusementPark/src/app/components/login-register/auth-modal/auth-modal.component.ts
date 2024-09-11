import {Component, Output, EventEmitter} from '@angular/core';
import {ApiService} from "../../../services/api.service";

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'] // Correction de styleUrl -> styleUrls
})
export class AuthModalComponent {
  @Output() closeModal = new EventEmitter<void>();

  constructor(private apiService: ApiService) {
  }

  onLoginSuccess() {
    this.closeModal.emit();
  }

  signInWithGoogle() {
    this.apiService.initiateGoogleLogin().subscribe((response) => {
      window.open(response.url, 'googleLogin', 'width=500,height=600');
    });
  }
}
