import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ApiService } from '../../../../../services/api.service';

@Component({
  selector: 'app-admin-park-items',
  templateUrl: './admin-park-items.component.html',
  styleUrls: ['./admin-park-items.component.scss']
})
export class AdminParkItemsComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  items: ParkItem[] = [];
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
    this.loadData();
  }

  loadData(): void {
    if (!this.parkId) {
      return;
    }

    this.loading = true;
    this.apiService.getParkZonesByParkId(this.parkId).subscribe((zones: ParkZone[]) => {
      this.zones = zones;
    });

    this.apiService.getParkItemsByParkId(this.parkId).subscribe({
      next: (items: ParkItem[]) => {
        this.items = items;
        this.loading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading park items', error);
        this.loading = false;
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

    this.apiService.deleteParkItem(item.id).subscribe({
      next: () => this.loadData(),
      error: (error: unknown) => console.error('Error deleting park item', error)
    });
  }
}
