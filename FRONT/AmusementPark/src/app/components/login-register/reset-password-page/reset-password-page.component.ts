import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../services/api.service';

@Component({
  selector: 'app-reset-password-page',
  templateUrl: './reset-password-page.component.html',
  styleUrls: ['./reset-password-page.component.scss'],
  standalone: false
})
export class ResetPasswordPageComponent implements OnInit {
  token: string = '';
  newPassword: string = '';
  confirmPassword: string = '';
  isSubmitted: boolean = false;
  message: string = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly apiService: ApiService) {
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  onSubmit(): void {
    this.apiService.resetPassword(this.token, this.newPassword, this.confirmPassword).subscribe({
      next: (response) => {
        this.isSubmitted = true;
        this.message = response.message;
      },
      error: (error) => {
        this.isSubmitted = true;
        this.message = error.error?.message ?? error.error?.Message ?? 'La réinitialisation du mot de passe a échoué.';
      }
    });
  }
}
