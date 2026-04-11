import { Component } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-forgot-password-page',
    templateUrl: './forgot-password-page.component.html',
    styleUrls: ['./forgot-password-page.component.scss'],
    imports: [Bind, Card, FormsModule, InputText, ButtonDirective, TranslateModule]
})
export class ForgotPasswordPageComponent {
  email: string = '';
  isSubmitted: boolean = false;
  message: string = '';

  constructor(private readonly authApiService: AuthApiService) {
  }

  onSubmit(): void {
    this.authApiService.forgotPassword(this.email).subscribe({
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
