import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';

import { Park } from '../../../../models/parks/park';
import { ParkItem } from '../../../../models/parks/park-item';
import { ApiService } from '../../../../services/api.service';

type ParkItemAdminRow = {
  parkId: string;
  parkName: string;
  itemId: string;
  itemName: string;
  itemType: string;
  isVisible: boolean;
};

@Component({
  selector: 'app-admin-park-items-index',
  templateUrl: './admin-park-items-index.component.html',
  styleUrls: ['./admin-park-items-index.component.scss']
})
export class AdminParkItemsIndexComponent implements OnInit {
  rows: ParkItemAdminRow[] = [];
  filteredRows: ParkItemAdminRow[] = [];
  parkOptions: { label: string; value: string | null }[] = [];
  loading: boolean = false;
  selectedParkId: string | null = null;
  searchTerm: string = '';

  constructor(
    private readonly apiService: ApiService,
    private readonly router: Router,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.loadRows();
  }

  loadRows(): void {
    this.loading = true;

    this.apiService.getParksPaginated(1, 1000).subscribe({
      next: (response: any) => {
        const parks: Park[] = response?.data ?? response?.items ?? [];
        const validParks: Park[] = parks.filter((park: Park) => !!park.id);

        this.parkOptions = [
          { label: this.translateService.instant('admin.parkItems.allParks'), value: null },
          ...validParks.map((park: Park) => ({
            label: park.name ?? '',
            value: park.id ?? null
          }))
        ];

        if (validParks.length === 0) {
          this.rows = [];
          this.applyFilters();
          this.loading = false;
          return;
        }

        const requests = validParks.map((park: Park) => this.apiService.getParkItemsByParkId(park.id ?? ''));

        forkJoin(requests).subscribe({
          next: (itemsByPark: ParkItem[][]) => {
            this.rows = validParks.flatMap((park: Park, index: number) => {
              const items: ParkItem[] = itemsByPark[index] ?? [];

              return items
                .filter((item: ParkItem) => !!item.id)
                .map((item: ParkItem) => ({
                  parkId: park.id ?? '',
                  parkName: park.name ?? '',
                  itemId: item.id ?? '',
                  itemName: item.name,
                  itemType: item.type,
                  isVisible: item.isVisible ?? true
                }));
            });

            this.applyFilters();
            this.loading = false;
          },
          error: (error: unknown) => {
            console.error('Error loading park items index', error);
            this.rows = [];
            this.applyFilters();
            this.loading = false;
          }
        });
      },
      error: (error: unknown) => {
        console.error('Error loading parks for park items index', error);
        this.rows = [];
        this.applyFilters();
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    const normalizedSearch: string = this.searchTerm.trim().toLowerCase();

    this.filteredRows = this.rows.filter((row: ParkItemAdminRow) => {
      const matchesPark: boolean = !this.selectedParkId || row.parkId === this.selectedParkId;
      const matchesSearch: boolean = normalizedSearch.length === 0
        || row.parkName.toLowerCase().includes(normalizedSearch)
        || row.itemName.toLowerCase().includes(normalizedSearch)
        || row.itemType.toLowerCase().includes(normalizedSearch);

      return matchesPark && matchesSearch;
    });
  }


  getTypeLabelKey(itemType: string): string {
    if (!itemType || itemType.length === 0) {
      return 'parkExplorer.types.other';
    }

    return `parkExplorer.types.${itemType.charAt(0).toLowerCase()}${itemType.slice(1)}`;
  }

  goToEdit(row: ParkItemAdminRow): void {
    const url: string = this.router.url;
    const lang: string = url.split('/')[1] || 'en';

    this.router.navigate(['/', lang, 'admin', 'parks', 'edit', row.parkId, 'items', row.itemId]);
  }
}
