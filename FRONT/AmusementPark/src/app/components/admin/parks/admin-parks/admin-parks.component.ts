import { Component, OnInit } from '@angular/core';
import {Park} from "../../../../models/parks/park";
import {Pagination} from "../../../../models/shared/pagination";
import {ApiService} from "../../../../services/api.service";
import {ParksApiResponse} from "../../../../models/parks/parks_api_response";

@Component({
  selector: 'app-admin-parks',
  templateUrl: './admin-parks.component.html',
  styleUrls: ['./admin-parks.component.scss']
})
export class AdminParksComponent implements OnInit {

  parks: Park[] = [];
  loading = false;

  pagination: Pagination | null = null;
  totalRecords = 0;
  pageSize = 10;
  currentPage = 1; // 1-based pour l'API

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadParks(this.currentPage, this.pageSize);
  }

  loadParks(page: number, size: number): void {
    this.loading = true;

    this.apiService.getParksPaginated(page, size).subscribe({
      next: (response: ParksApiResponse) => {
        this.parks = response.data ?? [];
        this.pagination = response.pagination ?? null;

        this.totalRecords = this.pagination?.totalItems ?? this.parks.length;
        this.pageSize = this.pagination?.itemsPerPage ?? size;
        this.currentPage = this.pagination?.currentPage ?? page;

        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading parks', err);
        this.loading = false;
      }
    });
  }

  // appelé par (onLazyLoad)
  onPageChanged(event: any): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;

    // console.log('Lazy load parks => page', page, 'rows', rows, 'event', event);

    this.loadParks(page, rows);
  }
}
