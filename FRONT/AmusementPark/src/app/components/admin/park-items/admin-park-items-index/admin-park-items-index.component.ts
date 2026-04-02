import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { forkJoin, Observable } from 'rxjs';

import { Park } from '../../../../models/parks/park';
import { ParkItemAdminRow } from '../../../../models/parks/park-item-admin-row';
import { ParksApiResponse } from '../../../../models/parks/parks_api_response';
import { ApiResponse } from '../../../../models/shared/api_reponse';
import { Pagination } from '../../../../models/shared/pagination';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-admin-park-items-index',
    templateUrl: './admin-park-items-index.component.html',
    styleUrls: ['./admin-park-items-index.component.scss'],
    standalone: false
})
export class AdminParkItemsIndexComponent implements OnInit {
  rows: ParkItemAdminRow[] = [];
  parkOptions: { label: string; value: string | null }[] = [];
  loading: boolean = false;
  selectedParkId: string | null = null;
  searchTerm: string = '';
  totalRecords: number = 0;
  currentPage: number = 1;
  pageSize: number = 20;

  constructor(
    private readonly apiService: ApiService,
    private readonly router: Router,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.loadParkOptions();
    this.loadRows();
  }

  loadRows(): void {
    this.loading = true;

    this.apiService.getParkItemsPaginated(
      this.currentPage,
      this.pageSize,
      this.selectedParkId,
      this.searchTerm
    ).subscribe({
      next: (response: ApiResponse<ParkItemAdminRow>) => {
        this.rows = response.data ?? [];
        this.totalRecords = response.pagination?.totalItems ?? this.rows.length;
        this.loading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading park items index', error);
        this.rows = [];
        this.totalRecords = 0;
        this.loading = false;
      }
    });
  }

  onFiltersChanged(): void {
    this.currentPage = 1;
    this.loadRows();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.loadRows();
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

    this.router.navigate(['/', lang, 'admin', 'parks', 'edit', row.parkId, 'items', row.id]);
  }

  private loadParkOptions(): void {
    this.getAllParks().subscribe({
      next: (parks: Park[]) => {
        const validParks: Park[] = parks.filter((park: Park) => !!park.id);
        this.parkOptions = [
          { label: this.translateService.instant('admin.parkItems.allParks'), value: null },
          ...validParks.map((park: Park) => ({
            label: park.name ?? '',
            value: park.id ?? null
          }))
        ];
      },
      error: (error: unknown) => {
        console.error('Error loading parks for park items filter', error);
        this.parkOptions = [
          { label: this.translateService.instant('admin.parkItems.allParks'), value: null }
        ];
      }
    });
  }

  private getAllParks(): Observable<Park[]> {
    return new Observable<Park[]>((observer) => {
      this.apiService.getParksPaginated(1, 100).subscribe({
        next: (firstResponse: ParksApiResponse) => {
          const firstPageParks: Park[] = firstResponse.data ?? [];
          const pagination: Pagination | undefined = firstResponse.pagination;
          const totalPages: number = pagination?.totalPages ?? 1;

          if (totalPages <= 1) {
            observer.next(firstPageParks);
            observer.complete();
            return;
          }

          const requests: Observable<ParksApiResponse>[] = [];
          for (let page: number = 2; page <= totalPages; page++) {
            requests.push(this.apiService.getParksPaginated(page, 100));
          }

          forkJoin(requests).subscribe({
            next: (responses: ParksApiResponse[]) => {
              const allParks: Park[] = [...firstPageParks];

              responses.forEach((response: ParksApiResponse) => {
                allParks.push(...(response.data ?? []));
              });

              observer.next(allParks);
              observer.complete();
            },
            error: (error: unknown) => {
              observer.error(error);
            }
          });
        },
        error: (error: unknown) => {
          observer.error(error);
        }
      });
    });
  }
}
