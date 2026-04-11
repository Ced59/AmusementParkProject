import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AttractionManufacturer } from '../../../../models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-manufacturers',
    templateUrl: './admin-manufacturers.component.html',
    styleUrls: ['./admin-manufacturers.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, PrimeTemplate, FormsModule, InputText, ButtonDirective, RouterLink, TableModule, TranslateModule]
})
export class AdminManufacturersComponent implements OnInit {
  manufacturers: AttractionManufacturer[] = [];
  filteredManufacturers: AttractionManufacturer[] = [];
  loading: boolean = false;
  searchQuery: string = '';
  currentLang: string = 'en';

  constructor(
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly route: ActivatedRoute,
    private readonly cdr: ChangeDetectorRef
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
    this.cdr.markForCheck();

    this.manufacturersApiService.getAttractionManufacturers().subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturers = manufacturers;
        this.applyFilter();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading manufacturers', error);
        this.loading = false;
        this.cdr.markForCheck();
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
