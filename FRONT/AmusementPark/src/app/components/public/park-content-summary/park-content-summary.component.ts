import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';

interface ParkContentSummaryEntry {
  labelKey: string;
  count: number;
  icon: string;
  queryParams?: Record<string, string>;
}

@Component({
  selector: 'app-park-content-summary',
  templateUrl: './park-content-summary.component.html',
  styleUrls: ['./park-content-summary.component.scss'],
  imports: [NgFor, NgIf, RouterLink, TranslateModule, ButtonDirective]
})
export class ParkContentSummaryComponent {
  @Input() park: Park | null = null;
  @Input() explorer: ParkExplorer | null = null;
  @Input() currentLang: string = 'en';

  get summaryEntries(): ParkContentSummaryEntry[] {
    if (!this.explorer) {
      return [];
    }

    const categoryCounts: Map<string, number> = new Map<string, number>(
      this.explorer.overview.countsByCategory.map((item: ParkExplorerCount) => [item.key, item.count])
    );
    const typeCounts: Map<string, number> = new Map<string, number>(
      this.explorer.overview.countsByType.map((item: ParkExplorerCount) => [item.key, item.count])
    );

    const entries: ParkContentSummaryEntry[] = [
      {
        labelKey: 'parkVisitor.summary.totalItems',
        count: this.explorer.overview.totalItems,
        icon: 'pi pi-th-large'
      },
      {
        labelKey: 'parkExplorer.categories.attraction',
        count: categoryCounts.get('Attraction') ?? 0,
        icon: 'pi pi-star',
        queryParams: { category: 'Attraction' }
      },
      {
        labelKey: 'parkExplorer.types.rollerCoaster',
        count: typeCounts.get('RollerCoaster') ?? 0,
        icon: 'pi pi-bolt',
        queryParams: { type: 'RollerCoaster' }
      },
      {
        labelKey: 'parkExplorer.types.waterRide',
        count: typeCounts.get('WaterRide') ?? 0,
        icon: 'pi pi-compass',
        queryParams: { type: 'WaterRide' }
      },
      {
        labelKey: 'parkExplorer.types.flatRide',
        count: typeCounts.get('FlatRide') ?? 0,
        icon: 'pi pi-sync',
        queryParams: { type: 'FlatRide' }
      },
      {
        labelKey: 'parkExplorer.types.darkRide',
        count: typeCounts.get('DarkRide') ?? 0,
        icon: 'pi pi-moon',
        queryParams: { type: 'DarkRide' }
      },
      {
        labelKey: 'parkExplorer.categories.restaurant',
        count: categoryCounts.get('Restaurant') ?? 0,
        icon: 'pi pi-shopping-cart',
        queryParams: { category: 'Restaurant' }
      },
      {
        labelKey: 'parkExplorer.categories.hotel',
        count: categoryCounts.get('Hotel') ?? 0,
        icon: 'pi pi-building',
        queryParams: { category: 'Hotel' }
      },
      {
        labelKey: 'parkExplorer.categories.show',
        count: categoryCounts.get('Show') ?? 0,
        icon: 'pi pi-ticket',
        queryParams: { category: 'Show' }
      },
      {
        labelKey: 'parkExplorer.categories.service',
        count: categoryCounts.get('Service') ?? 0,
        icon: 'pi pi-wrench',
        queryParams: { category: 'Service' }
      }
    ];

    return entries.filter((entry: ParkContentSummaryEntry) => entry.count > 0 || entry.queryParams == null);
  }

  get itemsLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name), 'items'];
  }
}
