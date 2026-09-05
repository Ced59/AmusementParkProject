export interface PassportGlobalBarChartRow {
  id: string;
  label: string | null;
  fallbackLabelKey?: string;
  detail?: string | null;
  primaryValue: number;
  secondaryValue?: number;
}
