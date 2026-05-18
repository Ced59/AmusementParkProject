export type ParkRegionFilter = 'europe' | 'north-america' | 'south-america' | 'orient' | 'africa';

export interface ParkRegionFilterOption {
  value: ParkRegionFilter | null;
  labelKey: string;
}
