export type ParkRegionFilter = 'europe' | 'north-america' | 'south-america' | 'asia' | 'middle-east' | 'oceania' | 'africa';

export interface ParkRegionFilterOption {
  value: ParkRegionFilter | null;
  labelKey: string;
}
