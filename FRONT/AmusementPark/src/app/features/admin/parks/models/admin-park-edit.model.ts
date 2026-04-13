import { ParkType } from '@app/models/parks/park-type';

export interface AdminParkTypeOption {
  labelKey: string;
  value: ParkType;
}

export interface AdminParkCountryOption {
  code: string;
  label: string;
}
