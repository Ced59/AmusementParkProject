import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-reset-password-page',
    templateUrl: './reset-password-page.component.html',
    styleUrls: ['./reset-password-page.component.scss'],
    imports: [Bind, Card, FormsModule, InputText, ButtonDirective, TranslateModule]
})
export class ResetPasswordPageComponent implements OnInit {
  token: string = '';
  newPassword: string = '';
  confirmPassword: string = '';
  isSubmitted: boolean = false;
  message: string = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly authApiService: AuthApiService) {
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
  }

  onSubmit(): void {
    this.authApiService.resetPassword(this.token, this.newPassword, this.confirmPassword).subscribe({
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
