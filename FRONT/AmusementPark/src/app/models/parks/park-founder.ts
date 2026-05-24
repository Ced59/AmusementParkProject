import { LocalizedItem } from '../shared/localized-item';

export interface ParkFounder {
  id?: string;
  name: string;
  occupation?: string | null;
  birthDate?: string | null;
  deathDate?: string | null;
  birthPlace?: string | null;
  nationalityCountryCode?: string | null;
  websiteUrl?: string | null;
  biography?: LocalizedItem<string>[];
}
