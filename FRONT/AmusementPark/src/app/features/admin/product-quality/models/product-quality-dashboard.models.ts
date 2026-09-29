export type ProductQualityProgramCode =
  | 'RANK'
  | 'PASS'
  | 'SHARE'
  | 'FIT'
  | 'WATCH'
  | 'TRIP'
  | 'HIST'
  | 'LIVE';

export type ProductQualityEvidenceState = 'ready' | 'split' | 'operational';

export type ProductQualitySignalChannel = 'firstParty' | 'mixed' | 'technical';

export interface ProductQualityDashboardDefinition {
  readonly channel: ProductQualitySignalChannel;
  readonly code: ProductQualityProgramCode;
  readonly iconClass: string;
  readonly id: string;
  readonly routeSegments: readonly string[];
  readonly state: ProductQualityEvidenceState;
}

export const PRODUCT_QUALITY_DASHBOARDS: readonly ProductQualityDashboardDefinition[] = [
  {
    id: 'rank',
    code: 'RANK',
    iconClass: 'pi pi-sort-amount-up',
    routeSegments: ['rating-rankings'],
    state: 'operational',
    channel: 'technical'
  },
  {
    id: 'pass',
    code: 'PASS',
    iconClass: 'pi pi-id-card',
    routeSegments: ['passport-beta'],
    state: 'ready',
    channel: 'firstParty'
  },
  {
    id: 'share',
    code: 'SHARE',
    iconClass: 'pi pi-share-alt',
    routeSegments: ['share-moderation'],
    state: 'split',
    channel: 'mixed'
  },
  {
    id: 'fit',
    code: 'FIT',
    iconClass: 'pi pi-compass',
    routeSegments: ['park-fit-pilot'],
    state: 'ready',
    channel: 'firstParty'
  },
  {
    id: 'watch',
    code: 'WATCH',
    iconClass: 'pi pi-bell',
    routeSegments: ['watch-pilot'],
    state: 'ready',
    channel: 'firstParty'
  },
  {
    id: 'trip',
    code: 'TRIP',
    iconClass: 'pi pi-map',
    routeSegments: ['trip-pilot'],
    state: 'ready',
    channel: 'firstParty'
  },
  {
    id: 'hist',
    code: 'HIST',
    iconClass: 'pi pi-history',
    routeSegments: ['history', 'diagnostics'],
    state: 'operational',
    channel: 'technical'
  },
  {
    id: 'live',
    code: 'LIVE',
    iconClass: 'pi pi-bolt',
    routeSegments: ['live-operations'],
    state: 'operational',
    channel: 'technical'
  }
];
