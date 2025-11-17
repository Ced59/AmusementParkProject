import { Component, OnInit } from '@angular/core';
import {Park} from "../../../../models/parks/park";
import {Pagination} from "../../../../models/shared/pagination";
import {ApiService} from "../../../../services/api.service";
import {ParksApiResponse} from "../../../../models/parks/parks_api_response";
import {ActivatedRoute, Router} from "@angular/router";

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
  currentPage = 1; // 1-based

  searchQuery = '';

  constructor(
    private apiService: ApiService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadParks(this.currentPage, this.pageSize);
  }

  loadParks(page: number, size: number): void {
    this.loading = true;

    const trimmedQuery = this.searchQuery.trim();

    const handleResponse = (response: ParksApiResponse, page: number, size: number) => {
      const rawParks = response.data ?? [];

      // 🔹 On force isVisible à false si absent
      this.parks = rawParks.map(p => ({
        ...p,
        isVisible: p.isVisible ?? false
      }));

      this.pagination = response.pagination ?? null;
      this.totalRecords = this.pagination?.totalItems ?? this.parks.length;
      this.pageSize = this.pagination?.itemsPerPage ?? size;
      this.currentPage = this.pagination?.currentPage ?? page;
      this.loading = false;
    };

    if (trimmedQuery.length > 0) {
      this.apiService.searchParks(trimmedQuery, page, size).subscribe({
        next: (response) => handleResponse(response, page, size),
        error: (err) => {
          console.error('Error searching parks', err);
          this.loading = false;
        }
      });
    } else {
      this.apiService.getParksPaginated(page, size).subscribe({
        next: (response) => handleResponse(response, page, size),
        error: (err) => {
          console.error('Error loading parks', err);
          this.loading = false;
        }
      });
    }
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadParks(this.currentPage, this.pageSize);
  }

  clearSearch(): void {
    if (!this.searchQuery.trim()) {
      return;
    }
    this.searchQuery = '';
    this.currentPage = 1;
    this.loadParks(this.currentPage, this.pageSize);
  }

  onPageChanged(event: any): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;
    this.loadParks(page, rows);
  }

  // 🔹 Changement de visibilité depuis le tableau
  onVisibilityChange(park: Park): void {
    if (!park.id) {
      return;
    }

    const newValue = !!park.isVisible;

    this.apiService.updateParkVisibility(park.id, newValue).subscribe({
      next: () => {
        // Optionnel : toaster, console.log, etc.
        // console.log(`Visibility updated for park ${park.id} => ${newValue}`);
      },
      error: (err) => {
        console.error('Error updating park visibility', err);
        // Optionnel : rollback local si tu veux
        park.isVisible = !newValue;
      }
    });
  }
}
