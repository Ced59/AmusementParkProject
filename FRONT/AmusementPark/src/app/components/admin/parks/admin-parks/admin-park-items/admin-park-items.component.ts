import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';

@Component({
    selector: 'app-admin-park-items',
    templateUrl: './admin-park-items.component.html',
    styleUrls: ['./admin-park-items.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, PrimeTemplate, ButtonDirective, RouterLink, TableModule, TranslateModule]
})
export class AdminParkItemsComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  items: ParkItem[] = [];
  zones: ParkZone[] = [];
  loading: boolean = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly translateService: TranslateService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.loadData();
  }

  loadData(): void {
    if (!this.parkId) {
      return;
    }

    this.loading = true;
    this.cdr.markForCheck();

    this.parkZonesApiService.getParkZonesByParkId(this.parkId).subscribe((zones: ParkZone[]) => {
      this.zones = zones;
      this.cdr.markForCheck();
    });

    this.parkItemsApiService.getParkItemsByParkId(this.parkId).subscribe({
      next: (items: ParkItem[]) => {
        this.items = items;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading park items', error);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getZoneName(zoneId?: string | null): string {
    if (!zoneId) {
      return '—';
    }

    const zone: ParkZone | undefined = this.zones.find((item: ParkZone) => item.id === zoneId);
    return resolveLocalizedValue(zone?.names, this.currentLang) ?? zone?.name ?? '—';
  }

  deleteItem(item: ParkItem): void {
    if (!item.id || !confirm(this.translateService.instant('admin.parks.items.deleteConfirm'))) {
      return;
    }

    this.parkItemsApiService.deleteParkItem(item.id).subscribe({
      next: () => this.loadData(),
      error: (error: unknown) => console.error('Error deleting park item', error)
    });
  }
}
