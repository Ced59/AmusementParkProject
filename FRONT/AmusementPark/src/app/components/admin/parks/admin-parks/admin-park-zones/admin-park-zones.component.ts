import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { ParkZone } from '@app/models/parks/park-zone';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { PageStateComponent } from '../../../../shared/page-state/page-state.component';
import { AdminParkZonesStateFacade } from '@features/admin/parks/state/admin-park-zones-state.facade';

@Component({
    selector: 'app-admin-park-zones',
    templateUrl: './admin-park-zones.component.html',
    styleUrls: ['./admin-park-zones.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminParkZonesStateFacade],
    imports: [Bind, Card, PrimeTemplate, ButtonDirective, RouterLink, TableModule, TranslateModule, PageStateComponent, EmptyStateComponent]
})
export class AdminParkZonesComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  protected readonly state = this.stateFacade.state;
  protected readonly zones = this.stateFacade.zones;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly translateService: TranslateService,
    private readonly stateFacade: AdminParkZonesStateFacade
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

    this.stateFacade.loadZones(this.parkId);
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
