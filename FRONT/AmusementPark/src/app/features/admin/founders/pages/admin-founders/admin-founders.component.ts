import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Card } from '@shared/primeless/card';
import { PrimeTemplate } from '@shared/primeless/api';
import { FormsModule } from '@angular/forms';
import { InputText } from '@shared/primeless/inputtext';
import { ButtonDirective } from '@shared/primeless/button';
import { TableModule } from '@shared/primeless/table';
import { Tag } from '@shared/primeless/tag';
import { PaginatorState } from '@shared/primeless/paginator';
import { TranslateModule } from '@ngx-translate/core';

import { ParkFounder } from '@app/models/parks/park-founder';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { AdminFoundersStateFacade } from '@features/admin/founders/state/admin-founders-state.facade';

@Component({
  selector: 'app-admin-founders',
  templateUrl: './admin-founders.component.html',
  styleUrls: ['./admin-founders.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminFoundersStateFacade],
  imports: [Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, Tag, TranslateModule, EmptyStateComponent, PaginationComponent]
})
export class AdminFoundersComponent implements OnInit {
  protected readonly founders = this.stateFacade.pagedFounders;
  protected readonly loading = this.stateFacade.loading;
  protected readonly totalCount = this.stateFacade.totalCount;
  protected readonly currentPage = this.stateFacade.currentPage;
  protected readonly pageSize = this.stateFacade.pageSize;
  currentLang: string = 'en';

  constructor(
    protected readonly stateFacade: AdminFoundersStateFacade,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.stateFacade.loadFounders();
  }

  onSearchQueryChanged(searchQuery: string): void {
    this.stateFacade.setSearchQuery(searchQuery);
  }

  onPageChanged(event: PaginatorState): void {
    const pageSize: number = event.rows ?? this.pageSize();
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / pageSize) + 1;
    this.stateFacade.setPage(page, pageSize);
  }

  hasLifeDates(founder: ParkFounder): boolean {
    return !!founder.birthDate || !!founder.deathDate;
  }

  getLifeDates(founder: ParkFounder): string {
    if (founder.birthDate && founder.deathDate) {
      return `${founder.birthDate} — ${founder.deathDate}`;
    }

    return founder.birthDate ?? founder.deathDate ?? '';
  }
}
