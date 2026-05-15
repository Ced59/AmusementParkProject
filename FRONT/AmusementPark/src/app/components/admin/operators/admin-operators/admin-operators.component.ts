import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { AdminOperatorsStateFacade } from '@features/admin/operators/state/admin-operators-state.facade';

@Component({
    selector: 'app-admin-operators',
    templateUrl: './admin-operators.component.html',
    styleUrls: ['./admin-operators.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminOperatorsStateFacade],
    imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, TranslateModule, EmptyStateComponent]
})
export class AdminOperatorsComponent implements OnInit {
  protected readonly filteredOperators = this.stateFacade.filteredOperators;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalCount = this.stateFacade.totalCount;
  currentLang: string = 'en';

  constructor(
    protected readonly stateFacade: AdminOperatorsStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.stateFacade.loadOperators();
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }
}
