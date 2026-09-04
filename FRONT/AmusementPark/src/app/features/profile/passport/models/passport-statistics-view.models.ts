export type PassportStatisticsScopeKind = 'item' | 'park' | 'year';

export interface PassportStatisticsRouteScope {
  kind: PassportStatisticsScopeKind;
  targetId: string;
}

export interface PassportStatisticCardViewModel {
  id: string;
  iconClass: string;
  labelKey: string;
  value: string;
  detailKey: string | null;
  detailParams: Readonly<Record<string, string | number>>;
}

export interface PassportStatisticsTimelinePointViewModel {
  id: string;
  visitId: string;
  dateLabel: string;
  ratingLabel: string;
  positionLabel: string | null;
}

export interface PassportStatisticsTableColumnViewModel {
  key: string;
  labelKey: string;
}

export interface PassportStatisticsTableCellViewModel {
  columnKey: string;
  value: string;
  translate?: boolean;
}

export interface PassportStatisticsNavigationViewModel {
  kind: 'visit' | 'item' | 'park' | 'year';
  targetId: string;
  labelKey: string;
}

export interface PassportStatisticsTableRowViewModel {
  id: string;
  cells: PassportStatisticsTableCellViewModel[];
  navigation: PassportStatisticsNavigationViewModel | null;
}

export interface PassportStatisticsTableViewModel {
  id: string;
  titleKey: string;
  descriptionKey: string;
  emptyKey: string;
  columns: PassportStatisticsTableColumnViewModel[];
  rows: PassportStatisticsTableRowViewModel[];
}

export interface PassportStatisticsTrendViewModel {
  kind: 'stable' | 'rising' | 'falling';
  labelKey: string;
  deltaLabel: string;
  firstAverageLabel: string;
  lastAverageLabel: string;
  firstCount: number;
  lastCount: number;
}

export interface PassportStatisticsViewModel {
  scope: PassportStatisticsRouteScope;
  title: string;
  subtitleKey: string;
  cards: PassportStatisticCardViewModel[];
  timelineTitleKey: string | null;
  timelineDescriptionKey: string | null;
  timeline: PassportStatisticsTimelinePointViewModel[];
  trend: PassportStatisticsTrendViewModel | null;
  tables: PassportStatisticsTableViewModel[];
  isEmpty: boolean;
}
