import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ApiService } from '../../../../../services/api.service';

@Component({
  selector: 'app-admin-park-zones',
  templateUrl: './admin-park-zones.component.html',
  styleUrls: ['./admin-park-zones.component.scss']
})
export class AdminParkZonesComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  zones: ParkZone[] = [];
  loading: boolean = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly apiService: ApiService,
    private readonly translateService: TranslateService
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
    this.apiService.getParkZonesByParkId(this.parkId).subscribe({
      next: (zones: ParkZone[]) => {
        this.zones = zones;
        this.loading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading zones', error);
        this.loading = false;
      }
    });
  }

  deleteZone(zone: ParkZone): void {
    if (!zone.id || !confirm(this.translateService.instant('admin.parks.zones.deleteConfirm'))) {
      return;
    }

    this.apiService.deleteParkZone(zone.id).subscribe({
      next: () => this.loadZones(),
      error: (error: unknown) => console.error('Error deleting zone', error)
    });
  }

  getZoneDisplayName(zone: ParkZone): string {
    return resolveLocalizedValue(zone.names, this.currentLang) ?? zone.name ?? '—';
  }
}
