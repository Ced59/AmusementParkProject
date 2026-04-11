import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { forkJoin, Observable } from 'rxjs';

import { Park } from '../../../../models/parks/park';
import { ParkItemAdminRow } from '../../../../models/parks/park-item-admin-row';
import { ParksApiResponse } from '../../../../models/parks/parks_api_response';
import { ApiResponse } from '../../../../models/shared/api_reponse';
import { Pagination } from '../../../../models/shared/pagination';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { PrimeTemplate } from 'primeng/api';
import { Tag } from 'primeng/tag';
import { ButtonDirective } from 'primeng/button';
import { Paginator } from 'primeng/paginator';

@Component({
    selector: 'app-admin-park-items-index',
    templateUrl: './admin-park-items-index.component.html',
    styleUrls: ['./admin-park-items-index.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, FormsModule, InputText, Select, TableModule, PrimeTemplate, Tag, ButtonDirective, Paginator, TranslateModule]
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
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly router: Router,
    private readonly translateService: TranslateService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.loadParkOptions();
    this.loadRows();
  }

  loadRows(): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.parkItemsApiService.getParkItemsPaginated(
      this.currentPage,
      this.pageSize,
      this.selectedParkId,
      this.searchTerm
    ).subscribe({
      next: (response: ApiResponse<ParkItemAdminRow>) => {
        this.rows = response.data ?? [];
        this.totalRecords = response.pagination?.totalItems ?? this.rows.length;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading park items index', error);
        this.rows = [];
        this.totalRecords = 0;
        this.loading = false;
        this.cdr.markForCheck();
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

  getTypeLabelKey(itemType: string | number | null | undefined): string {
    const normalizedType: string | null = typeof itemType === 'string'
      ? itemType
      : null;

    if (!normalizedType || normalizedType.length === 0) {
      return 'parkExplorer.types.other';
    }

    return `parkExplorer.types.${normalizedType.charAt(0).toLowerCase()}${normalizedType.slice(1)}`;
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
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading parks for park items filter', error);
        this.parkOptions = [
          { label: this.translateService.instant('admin.parkItems.allParks'), value: null }
        ];
        this.cdr.markForCheck();
      }
    });
  }

  private getAllParks(): Observable<Park[]> {
    return new Observable<Park[]>((observer) => {
      this.parksApiService.getParksPaginated(1, 100).subscribe({
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
            requests.push(this.parksApiService.getParksPaginated(page, 100));
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
