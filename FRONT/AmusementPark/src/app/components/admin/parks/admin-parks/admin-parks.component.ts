import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Park } from '../../../../models/parks/park';
import { Pagination } from '../../../../models/shared/pagination';
import { ApiService } from '../../../../services/api.service';
import { ParksApiResponse } from '../../../../models/parks/parks_api_response';
import { ActivatedRoute, Router } from '@angular/router';
import { ParkType } from '../../../../models/parks/park-type';

@Component({
  selector: 'app-admin-parks',
  templateUrl: './admin-parks.component.html',
  styleUrls: ['./admin-parks.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminParksComponent implements OnInit {
  parks: Park[] = [];
  loading: boolean = false;

  pagination: Pagination | null = null;
  totalRecords: number = 0;
  pageSize: number = 10;
  currentPage: number = 1;

  searchQuery: string = '';

  constructor(
    private readonly apiService: ApiService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadParks(this.currentPage, this.pageSize);
  }

  loadParks(page: number, size: number): void {
    this.loading = true;
    this.cdr.markForCheck();

    const trimmedQuery: string = this.searchQuery.trim();

    const handleResponse = (response: ParksApiResponse, currentPage: number, currentSize: number) => {
      const rawParks: Park[] = response.data ?? [];

      this.parks = rawParks.map((park: Park) => ({
        ...park,
        isVisible: park.isVisible ?? false
      }));

      this.pagination = response.pagination ?? null;
      this.totalRecords = this.pagination?.totalItems ?? this.parks.length;
      this.pageSize = this.pagination?.itemsPerPage ?? currentSize;
      this.currentPage = this.pagination?.currentPage ?? currentPage;
      this.loading = false;
      this.cdr.markForCheck();
    };

    if (trimmedQuery.length > 0) {
      this.apiService.searchParks(trimmedQuery, page, size).subscribe({
        next: (response: ParksApiResponse) => handleResponse(response, page, size),
        error: (error: unknown) => {
          console.error('Error searching parks', error);
          this.loading = false;
          this.cdr.markForCheck();
        }
      });
    } else {
      this.apiService.getParksPaginated(page, size).subscribe({
        next: (response: ParksApiResponse) => handleResponse(response, page, size),
        error: (error: unknown) => {
          console.error('Error loading parks', error);
          this.loading = false;
          this.cdr.markForCheck();
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
    const rows: number = event.rows ?? this.pageSize;
    const first: number = event.first ?? 0;
    const page: number = Math.floor(first / rows) + 1;
    this.loadParks(page, rows);
  }

  onVisibilityChange(park: Park): void {
    if (!park.id) {
      return;
    }

    const newValue: boolean = !!park.isVisible;

    this.apiService.updateParkVisibility(park.id, newValue).subscribe({
      next: () => {
      },
      error: (error: unknown) => {
        console.error('Error updating park visibility', error);
        park.isVisible = !newValue;
        this.cdr.markForCheck();
      }
    });
  }

  getTypeTranslationKey(type: ParkType | null | undefined): string {
    switch (type) {
      case 'ThemePark':
        return 'admin.parks.types.themePark';
      case 'WaterPark':
        return 'admin.parks.types.waterPark';
      case 'Zoo':
        return 'admin.parks.types.zoo';
      case 'AnimalPark':
        return 'admin.parks.types.animalPark';
      case 'AmusementPark':
        return 'admin.parks.types.amusementPark';
      case 'Resort':
        return 'admin.parks.types.resort';
      default:
        return 'admin.parks.types.notSpecified';
    }
  }
}
