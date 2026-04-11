import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';

@Component({
    selector: 'app-admin-park-zones',
    templateUrl: './admin-park-zones.component.html',
    styleUrls: ['./admin-park-zones.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, PrimeTemplate, ButtonDirective, RouterLink, TableModule, TranslateModule]
})
export class AdminParkZonesComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  zones: ParkZone[] = [];
  loading: boolean = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly translateService: TranslateService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.loadZones();
  }

  loadZones(): void {
    if (!this.parkId) {
      return;
    }

    this.loading = true;
    this.cdr.markForCheck();

    this.parkZonesApiService.getParkZonesByParkId(this.parkId).subscribe({
      next: (zones: ParkZone[]) => {
        this.zones = zones;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading zones', error);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  deleteZone(zone: ParkZone): void {
    if (!zone.id || !confirm(this.translateService.instant('admin.parks.zones.deleteConfirm'))) {
      return;
    }

    this.parkZonesApiService.deleteParkZone(zone.id).subscribe({
      next: () => this.loadZones(),
      error: (error: unknown) => console.error('Error deleting zone', error)
    });
  }

  getZoneDisplayName(zone: ParkZone): string {
    return resolveLocalizedValue(zone.names, this.currentLang) ?? zone.name ?? '—';
  }
}
