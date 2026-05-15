export interface ParkContentSummaryEntryViewModel {
  labelKey: string;
  count: number;
  icon: string;
  queryParams?: Record<string, string>;
}

export interface ParkContentSummaryViewModel {
  itemsLink: string[] | null;
  entries: ParkContentSummaryEntryViewModel[];
}
