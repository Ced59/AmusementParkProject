import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../../../services/api.service';
import { ParkOperator } from '../../../../models/parks/park-operator';

@Component({
  selector: 'app-admin-operators',
  templateUrl: './admin-operators.component.html',
  styleUrls: ['./admin-operators.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
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
