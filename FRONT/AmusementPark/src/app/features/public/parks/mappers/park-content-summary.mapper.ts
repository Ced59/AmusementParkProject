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
      'parkVisitor.summary.totalItems',
      explorer.overview.totalItems,
      'pi pi-th-large'
    ),
    createSummaryEntry(
      'parkExplorer.categories.attraction',
      categoryCounts.get('Attraction') ?? 0,
      'pi pi-star',
      { category: 'Attraction' }
    ),
    createSummaryEntry(
      'parkExplorer.types.rollerCoaster',
      typeCounts.get('RollerCoaster') ?? 0,
      'pi pi-bolt',
      { type: 'RollerCoaster' }
    ),
    createSummaryEntry(
      'parkExplorer.types.waterRide',
      typeCounts.get('WaterRide') ?? 0,
      'pi pi-compass',
      { type: 'WaterRide' }
    ),
    createSummaryEntry(
      'parkExplorer.types.flatRide',
      typeCounts.get('FlatRide') ?? 0,
      'pi pi-sync',
      { type: 'FlatRide' }
    ),
    createSummaryEntry(
      'parkExplorer.types.darkRide',
      typeCounts.get('DarkRide') ?? 0,
      'pi pi-moon',
      { type: 'DarkRide' }
    ),
    createSummaryEntry(
      'parkExplorer.categories.restaurant',
      categoryCounts.get('Restaurant') ?? 0,
      'pi pi-shopping-cart',
      { category: 'Restaurant' }
    ),
    createSummaryEntry(
      'parkExplorer.categories.hotel',
      categoryCounts.get('Hotel') ?? 0,
      'pi pi-building',
      { category: 'Hotel' }
    ),
    createSummaryEntry(
      'parkExplorer.categories.show',
      categoryCounts.get('Show') ?? 0,
      'pi pi-ticket',
      { category: 'Show' }
    ),
    createSummaryEntry(
      'parkExplorer.categories.service',
      categoryCounts.get('Service') ?? 0,
      'pi pi-wrench',
      { category: 'Service' }
    )
  ].filter((entry: ParkContentSummaryEntryViewModel) => entry.count > 0 || entry.queryParams == null);

  return {
    itemsLink: park.exploreLink,
    entries,
  };
}
