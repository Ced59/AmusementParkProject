import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { ResetPasswordPageStateFacade } from '@features/auth/state/reset-password-page-state.facade';

@Component({
    selector: 'app-reset-password-page',
    templateUrl: './reset-password-page.component.html',
    styleUrls: ['./reset-password-page.component.scss'],
    providers: [ResetPasswordPageStateFacade],
    imports: [Bind, Card, FormsModule, InputText, ButtonDirective, TranslateModule, PageStateComponent]
})
export class ResetPasswordPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly token = this.stateFacade.token;
  protected readonly newPassword = this.stateFacade.newPassword;
  protected readonly confirmPassword = this.stateFacade.confirmPassword;
  protected readonly isSubmitted = this.stateFacade.isSubmitted;
  protected readonly message = this.stateFacade.message;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: ResetPasswordPageStateFacade) {
  }

  ngOnInit(): void {
    const token: string = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.stateFacade.initialize(token);
  }

  onNewPasswordChanged(newPassword: string): void {
    this.stateFacade.setNewPassword(newPassword);
  }

  onConfirmPasswordChanged(confirmPassword: string): void {
    this.stateFacade.setConfirmPassword(confirmPassword);
  }

  onSubmit(): void {
    this.stateFacade.submit();
  }
}
