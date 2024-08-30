import { Component, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'] // Correction de styleUrl -> styleUrls
})
export class AuthModalComponent {
  @Output() closeModal = new EventEmitter<void>();

  onLoginSuccess() {
    this.closeModal.emit();
  }
}
