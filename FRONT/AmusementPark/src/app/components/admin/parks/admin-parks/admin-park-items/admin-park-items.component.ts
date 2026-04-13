import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state.component';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { PageStateComponent } from '../../../../shared/page-state/page-state.component';
import { AdminParkItemsStateFacade } from '@features/admin/parks/state/admin-park-items-state.facade';

@Component({
    selector: 'app-admin-park-items',
    templateUrl: './admin-park-items.component.html',
    styleUrls: ['./admin-park-items.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AdminParkItemsStateFacade],
    imports: [Bind, Card, PrimeTemplate, ButtonDirective, RouterLink, TableModule, TranslateModule, PageStateComponent, EmptyStateComponent]
})
export class AdminParkItemsComponent implements OnInit {
  parkId: string = '';
  currentLang: string = 'en';
  protected readonly state = this.stateFacade.state;
  protected readonly items = this.stateFacade.items;
  protected readonly zones = this.stateFacade.zones;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly translateService: TranslateService,
    private readonly stateFacade: AdminParkItemsStateFacade
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

    this.stateFacade.loadData(this.parkId);
  }

  getZoneName(zoneId?: string | null): string {
    if (!zoneId) {
      return '—';
    }

    const zone: ParkZone | undefined = this.zones().find((item: ParkZone) => item.id === zoneId);
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
