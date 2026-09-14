import { ParkFitSearchPark } from '@app/models/park-fit/park-fit-search.models';

export interface ParkFitComparisonSelection {
  park: ParkFitSearchPark;
  resultRank: number;
}

export interface ParkFitComparisonCell {
  parkId: string;
  primaryKey: string;
  primaryParams: Record<string, string | number>;
  secondaryKey: string | null;
  secondaryParams: Record<string, string | number>;
  linkUrl: string | null;
  fingerprint: string;
}

export interface ParkFitComparisonRow {
  id: string;
  labelKey: string;
  labelParams: Record<string, string | number>;
  iconClass: string;
  cells: ParkFitComparisonCell[];
  isDifferent: boolean;
}

export interface ParkFitComparisonSection {
  id: string;
  titleKey: string;
  rows: ParkFitComparisonRow[];
}
