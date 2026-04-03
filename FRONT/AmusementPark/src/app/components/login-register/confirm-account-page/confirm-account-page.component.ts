import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../services/api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-confirm-account-page',
    templateUrl: './confirm-account-page.component.html',
    styleUrls: ['./confirm-account-page.component.scss'],
    imports: [Bind, Card, RouterLink, TranslateModule]
})
export class ConfirmAccountPageComponent implements OnInit {
  currentLanguage: string = 'en';
  isLoading: boolean = true;
  isSuccess: boolean = false;
  message: string = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly apiService: ApiService) {
  }

  ngOnInit(): void {
    this.currentLanguage = this.route.parent?.snapshot.paramMap.get('lang') ?? 'en';

    const token: string = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!token) {
      this.isLoading = false;
      this.isSuccess = false;
      this.message = 'Le lien de confirmation est invalide.';
      return;
    }

    this.apiService.confirmEmail(token).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.isSuccess = true;
        this.message = response.message;
      },
      error: (error) => {
        this.isLoading = false;
        this.isSuccess = false;
        this.message = error.error?.message ?? error.error?.Message ?? 'La confirmation du compte a échoué.';
      }
    });
  }
}
