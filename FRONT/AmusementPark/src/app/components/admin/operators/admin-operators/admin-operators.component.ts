import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../../../services/api.service';
import { ParkOperator } from '../../../../models/parks/park-operator';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-operators',
    templateUrl: './admin-operators.component.html',
    styleUrls: ['./admin-operators.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, TranslateModule]
})
export class AdminOperatorsComponent implements OnInit {
  operators: ParkOperator[] = [];
  filteredOperators: ParkOperator[] = [];
  loading: boolean = false;
  searchQuery: string = '';
  currentLang: string = 'en';

  constructor(
    private readonly apiService: ApiService,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.loadOperators();
  }

  loadOperators(): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.apiService.getParkOperators().subscribe({
      next: (operators: ParkOperator[]) => {
        this.operators = operators;
        this.applyFilter();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading operators', error);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  applyFilter(): void {
    const normalizedQuery: string = this.searchQuery.trim().toLowerCase();

    if (!normalizedQuery) {
      this.filteredOperators = [...this.operators];
      return;
    }

    this.filteredOperators = this.operators.filter((parkOperator: ParkOperator) =>
      parkOperator.name.toLowerCase().includes(normalizedQuery));
  }
}
