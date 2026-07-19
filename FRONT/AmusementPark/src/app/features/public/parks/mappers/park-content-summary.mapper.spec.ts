import { ParkExplorer } from '@app/models/parks/park-explorer';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { mapParkContentSummaryViewModel } from './park-content-summary.mapper';

describe('mapParkContentSummaryViewModel', () => {
  it('uses plural-aware label keys for totals, categories and attraction types', () => {
    const park: ParkDetailViewModel = { exploreLink: ['/fr/parc/1/demo/lieux'] } as ParkDetailViewModel;
    const explorer: ParkExplorer = {
      parkId: '1',
      hasZones: false,
      overview: {
        name: 'Demo',
        isVirtual: false,
        totalItems: 3,
        countsByCategory: [
          { key: 'Show', count: 1 },
          { key: 'Shop', count: 2 }
        ],
        countsByType: [{ key: 'RollerCoaster', count: 1 }]
      },
      zones: []
    };

    const summary: ParkContentSummaryViewModel | null = mapParkContentSummaryViewModel(park, explorer);

    const publicCountsPrefix: string = ['publicCounts', ''].join('.');
    const homeCountsPrefix: string = ['home', 'counts', ''].join('.');
    expect(summary?.entries).toContain(jasmine.objectContaining({ labelKey: `${publicCountsPrefix}place`, count: 3 }));
    expect(summary?.entries).toContain(jasmine.objectContaining({ labelKey: `${homeCountsPrefix}show`, count: 1 }));
    expect(summary?.entries).toContain(jasmine.objectContaining({ labelKey: `${homeCountsPrefix}shop`, count: 2 }));
    expect(summary?.entries).toContain(jasmine.objectContaining({ labelKey: `${publicCountsPrefix}rollerCoaster`, count: 1 }));
  });
});
