export interface ParkDetailInfoRowViewModel {
  labelKey: string;
  value: string | number | null;
  valueKey?: string | null;
  externalUrl?: string | null;
  routerLink?: string[] | null;
  iconClass: string;
  isMonospace?: boolean;
}
