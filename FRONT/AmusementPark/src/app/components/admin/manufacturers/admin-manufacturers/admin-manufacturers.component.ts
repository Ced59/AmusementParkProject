import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AttractionManufacturer } from '../../../../models/parks/attraction-manufacturer';
import { ApiService } from '../../../../services/api.service';

@Component({
    selector: 'app-admin-manufacturers',
    templateUrl: './admin-manufacturers.component.html',
    styleUrls: ['./admin-manufacturers.component.scss'],
    standalone: false
})
export class AdminManufacturersComponent implements OnInit {
  manufacturers: AttractionManufacturer[] = [];
  filteredManufacturers: AttractionManufacturer[] = [];
  loading: boolean = false;
  searchQuery: string = '';
  currentLang: string = 'en';

  constructor(
    private readonly apiService: ApiService,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.loadManufacturers();
  }

  loadManufacturers(): void {
    this.loading = true;

    this.apiService.getAttractionManufacturers().subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturers = manufacturers;
        this.applyFilter();
        this.loading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading manufacturers', error);
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const normalizedQuery: string = this.searchQuery.trim().toLowerCase();

    if (!normalizedQuery) {
      this.filteredManufacturers = [...this.manufacturers];
      return;
    }

    this.filteredManufacturers = this.manufacturers.filter((manufacturer: AttractionManufacturer) =>
      manufacturer.name.toLowerCase().includes(normalizedQuery));
  }
}
