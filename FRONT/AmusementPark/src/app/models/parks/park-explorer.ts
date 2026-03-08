import { LocalizedItem } from '../shared/localized-item';

export interface ParkExplorerCount {
  key: string;
  count: number;
}

export interface ParkExplorerBucket {
  id?: string | null;
  name: string;
  names?: LocalizedItem<string>[];
  slug?: string | null;
  isVirtual: boolean;
  totalItems: number;
  countsByCategory: ParkExplorerCount[];
  countsByType: ParkExplorerCount[];
}

export interface ParkExplorer {
  parkId: string;
  hasZones: boolean;
  overview: ParkExplorerBucket;
  zones: ParkExplorerBucket[];
  unassigned?: ParkExplorerBucket | null;
}
