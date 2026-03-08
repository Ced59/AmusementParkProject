import { LocalizedItem } from '../shared/localized-item';

export interface ParkOperator {
  id?: string;
  name: string;
  description?: LocalizedItem<string>[];
}
