import { Component } from '@angular/core';
import { ApiService } from '../../../services/api.service';

@Component({
  selector: 'app-forgot-password-page',
  templateUrl: './forgot-password-page.component.html',
  styleUrls: ['./forgot-password-page.component.scss'],
  standalone: false
})
export class ForgotPasswordPageComponent {
  email: string = '';
  isSubmitted: boolean = false;
  message: string = '';

  constructor(private readonly apiService: ApiService) {
  }

  onSubmit(): void {
    this.apiService.forgotPassword(this.email).subscribe({
      next: (response) => {
        this.isSubmitted = true;
        this.message = response.message;
      },
      error: (error) => {
        this.isSubmitted = true;
        this.message = error.error?.message ?? error.error?.Message ?? 'La demande a échoué.';
      }
    });
  }
}
