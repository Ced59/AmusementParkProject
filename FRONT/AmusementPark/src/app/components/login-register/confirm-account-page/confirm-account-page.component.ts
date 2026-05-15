import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { ConfirmAccountPageStateFacade } from '@features/auth/state/confirm-account-page-state.facade';

@Component({
    selector: 'app-confirm-account-page',
    templateUrl: './confirm-account-page.component.html',
    styleUrls: ['./confirm-account-page.component.scss'],
    providers: [ConfirmAccountPageStateFacade],
    imports: [Bind, Card, RouterLink, TranslateModule, PageStateComponent]
})
export class ConfirmAccountPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly currentLanguage = this.stateFacade.currentLanguage;
  protected readonly isSuccess = this.stateFacade.isSuccess;
  protected readonly message = this.stateFacade.message;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly stateFacade: ConfirmAccountPageStateFacade
  ) {
  }

  ngOnInit(): void {
    const currentLanguage: string = this.route.parent?.snapshot.paramMap.get('lang') ?? 'en';
    const token: string = this.route.snapshot.queryParamMap.get('token') ?? '';

    this.stateFacade.confirmEmail(token, currentLanguage);
  }
}
