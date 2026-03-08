import { LocalizedItem } from '../shared/localized-item';

export interface ParkFounder {
  id?: string;
  name: string;
  biography?: LocalizedItem<string>[];
}
