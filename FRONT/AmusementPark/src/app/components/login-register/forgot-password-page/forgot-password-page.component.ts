import { Component } from '@angular/core';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { ForgotPasswordPageStateFacade } from '@features/auth/state/forgot-password-page-state.facade';

@Component({
    selector: 'app-forgot-password-page',
    templateUrl: './forgot-password-page.component.html',
    styleUrls: ['./forgot-password-page.component.scss'],
    providers: [ForgotPasswordPageStateFacade],
    imports: [Bind, Card, FormsModule, InputText, ButtonDirective, TranslateModule, PageStateComponent]
})
export class ForgotPasswordPageComponent {
  protected readonly state = this.stateFacade.state;
  protected readonly email = this.stateFacade.email;
  protected readonly isSubmitted = this.stateFacade.isSubmitted;
  protected readonly message = this.stateFacade.message;

  constructor(private readonly stateFacade: ForgotPasswordPageStateFacade) {
  }

  onEmailChanged(email: string): void {
    this.stateFacade.setEmail(email);
  }

  onSubmit(): void {
    this.stateFacade.submit();
  }
}
