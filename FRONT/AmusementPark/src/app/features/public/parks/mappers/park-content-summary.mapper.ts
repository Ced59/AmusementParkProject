import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkContentSummaryEntryViewModel, ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';

function createSummaryEntry(
  labelKey: string,
  count: number,
  icon: string,
  queryParams?: Record<string, string>
): ParkContentSummaryEntryViewModel {
  return {
    labelKey,
    count,
    icon,
    queryParams,
  };
}

function getPublicCountKey(segment: string): string {
  return `publicCounts.${segment}`;
}

function getHomeCountKey(segment: string): string {
  return `home.counts.${segment}`;
}

export function mapParkContentSummaryViewModel(
  park: ParkDetailViewModel | null,
  explorer: ParkExplorer | null
): ParkContentSummaryViewModel | null {
  if (!park?.exploreLink || !explorer) {
    return null;
  }

  const categoryCounts: Map<string, number> = new Map<string, number>(
    explorer.overview.countsByCategory.map((item: ParkExplorerCount) => [item.key, item.count])
  );
  const typeCounts: Map<string, number> = new Map<string, number>(
    explorer.overview.countsByType.map((item: ParkExplorerCount) => [item.key, item.count])
  );

  const entries: ParkContentSummaryEntryViewModel[] = [
    createSummaryEntry(
      getPublicCountKey('place'),
      explorer.overview.totalItems,
      'pi pi-th-large'
    ),
    createSummaryEntry(
      getHomeCountKey('attraction'),
      categoryCounts.get('Attraction') ?? 0,
      'pi pi-star',
      { category: 'Attraction' }
    ),
    createSummaryEntry(
      getPublicCountKey('rollerCoaster'),
      typeCounts.get('RollerCoaster') ?? 0,
      'pi pi-bolt',
      { type: 'RollerCoaster' }
    ),
    createSummaryEntry(
      getPublicCountKey('waterRide'),
      typeCounts.get('WaterRide') ?? 0,
      'pi pi-compass',
      { type: 'WaterRide' }
    ),
    createSummaryEntry(
      getPublicCountKey('flatRide'),
      typeCounts.get('FlatRide') ?? 0,
      'pi pi-sync',
      { type: 'FlatRide' }
    ),
    createSummaryEntry(
      getPublicCountKey('darkRide'),
      typeCounts.get('DarkRide') ?? 0,
      'pi pi-moon',
      { type: 'DarkRide' }
    ),
    createSummaryEntry(
      getHomeCountKey('restaurant'),
      categoryCounts.get('Restaurant') ?? 0,
      'pi pi-shopping-cart',
      { category: 'Restaurant' }
    ),
    createSummaryEntry(
      getHomeCountKey('hotel'),
      categoryCounts.get('Hotel') ?? 0,
      'pi pi-building',
      { category: 'Hotel' }
    ),
    createSummaryEntry(
      getHomeCountKey('show'),
      categoryCounts.get('Show') ?? 0,
      'pi pi-ticket',
      { category: 'Show' }
    ),
    createSummaryEntry(
      getHomeCountKey('shop'),
      categoryCounts.get('Shop') ?? 0,
      'pi pi-shopping-bag',
      { category: 'Shop' }
    ),
    createSummaryEntry(
      getHomeCountKey('animal'),
      categoryCounts.get('Animal') ?? 0,
      'pi pi-heart',
      { category: 'Animal' }
    ),
    createSummaryEntry(
      getHomeCountKey('transport'),
      categoryCounts.get('Transport') ?? 0,
      'pi pi-car',
      { category: 'Transport' }
    ),
    createSummaryEntry(
      getHomeCountKey('service'),
      categoryCounts.get('Service') ?? 0,
      'pi pi-wrench',
      { category: 'Service' }
    ),
    createSummaryEntry(
      getHomeCountKey('other'),
      categoryCounts.get('Other') ?? 0,
      'pi pi-ellipsis-h',
      { category: 'Other' }
    )
  ].filter((entry: ParkContentSummaryEntryViewModel) => entry.count > 0 || entry.queryParams == null);

  return {
    itemsLink: park.exploreLink,
    entries,
  };
}
