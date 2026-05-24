export interface UiCardActionModel {
  labelKey: string;
  iconClass?: string;
  routerLink?: string[] | null;
  externalUrl?: string | null;
  ariaLabelKey?: string;
}

export interface UiInfoCardMetricModel {
  labelKey?: string;
  labelText?: string;
  value: string | number | null;
  iconClass?: string;
}
