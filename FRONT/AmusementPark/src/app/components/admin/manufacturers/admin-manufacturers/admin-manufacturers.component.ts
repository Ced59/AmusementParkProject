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
import { AdminManufacturersStateFacade } from '@features/admin/manufacturers/state/admin-manufacturers-state.facade';

@Component({
    selector: 'app-admin-manufacturers',
    templateUrl: './admin-manufacturers.component.html',
    styleUrls: ['./admin-manufacturers.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminManufacturersStateFacade],
    imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, TranslateModule]
})
export class AdminManufacturersComponent implements OnInit {
  protected readonly filteredManufacturers = this.stateFacade.filteredManufacturers;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalCount = this.stateFacade.totalCount;
  currentLang: string = 'en';

  constructor(
    protected readonly stateFacade: AdminManufacturersStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.stateFacade.loadManufacturers();
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }
}
